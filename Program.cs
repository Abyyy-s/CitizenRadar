using OpenCvSharp;
using CitizenRadar;
using CitizenRadar.Core;
using CitizenRadar.Web;

// ── CUDA Dependency Setup ─────────────────────────────────────────────────────
// ONNX Runtime's CUDA execution provider (onnxruntime_providers_cuda.dll) needs
// cudart64_12.dll, cudnn64_9.dll, cublas64_12.dll, etc. on the PATH.
// There is no system-wide CUDA toolkit installed.  The required CUDA 12 + cuDNN 9
// DLLs are present in the PyTorch installation's torch\lib directory.
// We prepend that folder to the in-process PATH before any ORT session is created.
// This does NOT change any system or user environment variable permanently.
var torchCudaBin = @"C:\Users\abrah\AppData\Local\Programs\Python\Python311\Lib\site-packages\torch\lib";
if (Directory.Exists(torchCudaBin))
{
    Environment.SetEnvironmentVariable(
        "PATH",
        torchCudaBin + ";" + Environment.GetEnvironmentVariable("PATH"));
    Console.WriteLine($"[CUDA Setup] Prepended to PATH: {torchCudaBin}");
}
else
{
    Console.Error.WriteLine($"[CUDA Setup] WARNING: PyTorch torch\\lib not found at '{torchCudaBin}'.");
    Console.Error.WriteLine("             ONNX Runtime CUDA provider will likely fail to load its dependencies.");
    Console.Error.WriteLine("             Update 'torchCudaBin' in Program.cs to match your installation.");
}
// ─────────────────────────────────────────────────────────────────────────────

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.ConfigureKestrel(options => options.Limits.MaxRequestBodySize = 500 * 1024 * 1024);

// Parse CLI Arguments
string videoPath = args.Length > 0 ? args[0] : "data/traffic.mp4";
string modelPath = args.Length > 1 ? args[1] : Config.DefaultModelPath;
string calibPath = args.Length > 2 ? args[2] : Config.DefaultCalibrationPath;

// Verify files
if (!File.Exists(videoPath))
{
    Console.Error.WriteLine($"ERROR: Video file not found: {videoPath}");
    return;
}

if (!File.Exists(modelPath))
{
    Console.Error.WriteLine($"ERROR: ONNX model not found: {modelPath}");
    return;
}

// Register Web & Watchdog Services
builder.Services.AddSingleton<FrameBroadcaster>();
builder.Services.AddSingleton<TelemetryStore>();
builder.Services.AddSingleton(sp => new WatchdogService(
    sp.GetRequiredService<FrameBroadcaster>(),
    sp.GetRequiredService<TelemetryStore>(),
    videoPath,
    modelPath,
    calibPath
));
builder.Services.AddHostedService(sp => sp.GetRequiredService<WatchdogService>());

var app = builder.Build();

// Static Files for Web Dashboard
app.UseDefaultFiles();
app.UseStaticFiles();

// REST Endpoints
app.MapGet("/api/stream", async (FrameBroadcaster broadcaster, HttpResponse response, CancellationToken ct) =>
{
    await broadcaster.WriteMjpegStreamAsync(response, ct);
});

app.MapGet("/api/status", (TelemetryStore store) =>
{
    return Results.Ok(store.GetSnapshot());
});

app.MapGet("/api/violations", (TelemetryStore store) =>
{
    return Results.Ok(store.GetRecentViolations());
});

app.MapPost("/api/config", (TelemetryStore store, ConfigUpdateRequest req) =>
{
    if (req.SpeedLimitKmh > 0)
    {
        store.SpeedLimitKmh = req.SpeedLimitKmh;
    }
    return Results.Ok(new { success = true, speedLimitKmh = store.SpeedLimitKmh });
});

app.MapPost("/api/calibrate", (WatchdogService watchdog, TelemetryStore store, CalibrationRequest req) =>
{
    if (req.SourcePoints == null || req.SourcePoints.Length != 4)
    {
        return Results.BadRequest("Exactly 4 source points are required.");
    }

    var cvPoints = req.SourcePoints.Select(p => new Point2f(p.X, p.Y)).ToArray();
    watchdog.ApplyNewCalibration(cvPoints, req.RealWorldWidthMetres, req.RealWorldHeightMetres);
    store.NeedsCalibration = false;
    return Results.Ok(new { success = true, message = "Road geometry calibrated and applied." });
});

app.MapPost("/api/source/upload", async (HttpRequest request, WatchdogService watchdog, TelemetryStore store) =>
{
    var form = await request.ReadFormAsync();
    var file = form.Files.GetFile("file");
    if (file == null || file.Length == 0)
        return Results.BadRequest("No file uploaded.");
    
    var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
    var allowed = new[] { ".mp4", ".avi", ".mkv", ".mov", ".webm" };
    if (!allowed.Contains(ext))
        return Results.BadRequest($"Unsupported format: {ext}");
    
    var uploadDir = Path.Combine("data", "uploads");
    Directory.CreateDirectory(uploadDir);
    var savePath = Path.Combine(uploadDir, file.FileName);
    
    using (var stream = new FileStream(savePath, FileMode.Create))
    {
        await file.CopyToAsync(stream);
    }
    
    watchdog.SwitchSource(savePath);
    store.SourceName = file.FileName;
    store.SourceMode = "file";
    
    return Results.Ok(new { success = true, source = file.FileName });
}).DisableAntiforgery();

app.MapPost("/api/source/live", (WatchdogService watchdog, LiveSourceRequest req) =>
{
    watchdog.SwitchSource($"camera:{req.Device}");
    return Results.Ok(new { success = true, mode = "camera", device = req.Device });
});

app.MapGet("/api/source", (WatchdogService watchdog, TelemetryStore telemetry) =>
{
    return Results.Ok(new { source = watchdog.CurrentSource, mode = telemetry.SourceMode });
});

Console.WriteLine("╔══════════════════════════════════════════════════════════════════════════════╗");
Console.WriteLine("║                          CitizenRadar v1.0                                   ║");
Console.WriteLine("║                 AI Traffic Watchdog & Web Dashboard                          ║");
Console.WriteLine("╠══════════════════════════════════════════════════════════════════════════════╣");
Console.WriteLine("║  🌐 Web Dashboard: http://localhost:5000                                     ║");
Console.WriteLine("║  📡 MJPEG Stream:   http://localhost:5000/api/stream                          ║");
Console.WriteLine("║  📱 Mobile/LAN:    http://0.0.0.0:5000 (access from any phone on WiFi)       ║");
Console.WriteLine("╚══════════════════════════════════════════════════════════════════════════════╝");
Console.WriteLine();

app.Run("http://0.0.0.0:5000");

// Request DTOs
public record ConfigUpdateRequest(double SpeedLimitKmh);
public record PointDto(float X, float Y);
public record CalibrationRequest(PointDto[] SourcePoints, double RealWorldWidthMetres, double RealWorldHeightMetres);
public record LiveSourceRequest(int Device = 0);


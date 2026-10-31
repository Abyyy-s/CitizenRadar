using System.Collections.Concurrent;

namespace CitizenRadar.Web;

public record ViolationEvent(
    int Frame,
    int TrackId,
    string ClassName,
    double SpeedKmh,
    double SpeedLimitKmh,
    string Timestamp
);

public record TelemetrySnapshot(
    bool IsRunning,
    double Fps,
    int CurrentFrame,
    int TotalFrames,
    int TotalVehicles,
    int ActiveVehicles,
    double AverageSpeedKmh,
    int TotalViolations,
    double ViolationPercentage,
    double SpeedLimitKmh,
    string SourceName,
    string SourceMode,
    bool NeedsCalibration,
    string InferenceEngine,
    bool IsGpuAccelerated
);

/// <summary>
/// Thread-safe in-memory store for live traffic analytics, violation history,
/// and watchdog configuration.
/// </summary>
public class TelemetryStore
{
    private readonly ConcurrentQueue<ViolationEvent> _recentViolations = new();
    private const int MaxStoredViolations = 50;

    private readonly object _lock = new();

    public bool IsRunning { get; set; } = true;
    public double Fps { get; set; } = 30.0;
    public int CurrentFrame { get; set; } = 0;
    public int TotalFrames { get; set; } = 0;
    public int TotalVehicles { get; set; } = 0;
    public int ActiveVehicles { get; set; } = 0;
    public double AverageSpeedKmh { get; set; } = 0.0;
    public int TotalViolations { get; set; } = 0;
    public double ViolationPercentage { get; set; } = 0.0;
    public double SpeedLimitKmh { get; set; } = 40.0;
    public string SourceName { get; set; } = "data/traffic.mp4";
    public string SourceMode { get; set; } = "file";
    public bool NeedsCalibration { get; set; } = false;
    public string InferenceEngine { get; set; } = "ONNX Runtime";
    public bool IsGpuAccelerated { get; set; } = false;

    public void RecordViolation(int frame, int trackId, string className, double speed, double limit)
    {
        var ev = new ViolationEvent(
            frame,
            trackId,
            className,
            Math.Round(speed, 1),
            limit,
            DateTime.Now.ToString("HH:mm:ss")
        );

        _recentViolations.Enqueue(ev);

        // Keep queue size bounded
        while (_recentViolations.Count > MaxStoredViolations)
        {
            _recentViolations.TryDequeue(out _);
        }
    }

    public List<ViolationEvent> GetRecentViolations()
    {
        return _recentViolations.Reverse().ToList();
    }

    public void Reset()
    {
        lock (_lock)
        {
            TotalVehicles = 0;
            ActiveVehicles = 0;
            AverageSpeedKmh = 0.0;
            TotalViolations = 0;
            ViolationPercentage = 0.0;
            CurrentFrame = 0;
            NeedsCalibration = true;
            _recentViolations.Clear();
        }
    }

    public TelemetrySnapshot GetSnapshot()
    {
        lock (_lock)
        {
            return new TelemetrySnapshot(
                IsRunning,
                Math.Round(Fps, 1),
                CurrentFrame,
                TotalFrames,
                TotalVehicles,
                ActiveVehicles,
                Math.Round(AverageSpeedKmh, 1),
                TotalViolations,
                Math.Round(ViolationPercentage, 1),
                SpeedLimitKmh,
                SourceName,
                SourceMode,
                NeedsCalibration,
                InferenceEngine,
                IsGpuAccelerated
            );
        }
    }
}

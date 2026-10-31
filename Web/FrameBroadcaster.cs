using System.Threading.Channels;
using OpenCvSharp;

namespace CitizenRadar.Web;

/// <summary>
/// Thread-safe broadcaster that converts processed OpenCV Mat frames into
/// JPEG images and streams them to browser clients using standard MJPEG.
/// </summary>
public class FrameBroadcaster
{
    private byte[]? _latestFrameJpeg;
    private readonly object _lock = new();
    private long _frameSequence = 0;
    private long _lastEncodeTimestamp;

    /// <summary>
    /// Encodes and updates the latest processed frame.
    /// </summary>
    public void Broadcast(Mat frame)
    {
        if (frame.Empty()) return;

        long now = System.Diagnostics.Stopwatch.GetTimestamp();
        long minInterval = System.Diagnostics.Stopwatch.Frequency / Config.StreamMaxFps;
        if (now - Interlocked.Read(ref _lastEncodeTimestamp) < minInterval)
            return;
        Interlocked.Exchange(ref _lastEncodeTimestamp, now);

        // Limit browser encoding so CPU time stays available for detection and tracking.
        Cv2.ImEncode(".jpg", frame, out byte[] jpegBytes, new ImageEncodingParam(ImwriteFlags.JpegQuality, 65));

        lock (_lock)
        {
            _latestFrameJpeg = jpegBytes;
            _frameSequence++;
        }
    }

    /// <summary>
    /// Returns the current latest JPEG bytes, or null if none available yet.
    /// </summary>
    public byte[]? GetLatestJpeg()
    {
        lock (_lock)
        {
            return _latestFrameJpeg;
        }
    }

    /// <summary>
    /// Streams continuous MJPEG frames to an HTTP client response.
    /// Standard multipart/x-mixed-replace supported natively by all browsers.
    /// </summary>
    public async Task WriteMjpegStreamAsync(HttpResponse response, CancellationToken ct)
    {
        response.ContentType = "multipart/x-mixed-replace; boundary=frame";
        response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
        response.Headers["Pragma"] = "no-cache";
        response.Headers["Expires"] = "0";

        var outputStream = response.Body;
        long lastSentSeq = -1;

        while (!ct.IsCancellationRequested)
        {
            byte[]? currentJpeg = null;
            long currentSeq = 0;

            lock (_lock)
            {
                if (_frameSequence != lastSentSeq && _latestFrameJpeg != null)
                {
                    currentJpeg = _latestFrameJpeg;
                    currentSeq = _frameSequence;
                }
            }

            if (currentJpeg != null)
            {
                lastSentSeq = currentSeq;

                var header = System.Text.Encoding.ASCII.GetBytes(
                    $"--frame\r\nContent-Type: image/jpeg\r\nContent-Length: {currentJpeg.Length}\r\n\r\n");
                var footer = System.Text.Encoding.ASCII.GetBytes("\r\n");

                try
                {
                    await outputStream.WriteAsync(header, ct);
                    await outputStream.WriteAsync(currentJpeg, ct);
                    await outputStream.WriteAsync(footer, ct);
                    await outputStream.FlushAsync(ct);
                }
                catch
                {
                    // Client disconnected
                    break;
                }
            }

            // Yield CPU (~30-60 fps check interval)
            await Task.Delay(25, ct);
        }
    }
}

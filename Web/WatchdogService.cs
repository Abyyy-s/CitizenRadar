using OpenCvSharp;
using CitizenRadar.Core;
using CitizenRadar.Models;
using Tracker = CitizenRadar.Core.Tracker;

namespace CitizenRadar.Web;

/// <summary>
/// Background worker service that runs the end-to-end Computer Vision pipeline
/// (VideoReader → YOLOv8 Detector → IoU Tracker → Homography SpeedEstimator → Visualizer).
/// Feeds live annotated frames to the FrameBroadcaster and metrics to the TelemetryStore.
/// </summary>
public class WatchdogService : BackgroundService
{
    private readonly FrameBroadcaster _broadcaster;
    private readonly TelemetryStore _telemetry;
    private string _videoPath;
    private readonly string _modelPath;
    private readonly string _calibPath;

    private SpeedEstimator? _speedEstimator;
    private Mat? _currentHomography;
    private double _realWidth;
    private double _realHeight;
    private readonly object _calibLock = new();

    private volatile bool _sourceChanged = false;
    private string _pendingSource = "";
    private readonly object _sourceLock = new();

    // Debounce violation notifications per track ID
    private readonly HashSet<int> _notifiedViolationTracks = new();

    public string CurrentSource => _videoPath;

    public WatchdogService(
        FrameBroadcaster broadcaster,
        TelemetryStore telemetry,
        string videoPath = "data/traffic.mp4",
        string modelPath = "models/yolov8n.onnx",
        string calibPath = "data/calibration.json")
    {
        _broadcaster = broadcaster;
        _telemetry = telemetry;
        _videoPath = videoPath;
        _modelPath = modelPath;
        _calibPath = calibPath;
    }

    public void SwitchSource(string newSource)
    {
        lock (_sourceLock)
        {
            _pendingSource = newSource;
            _sourceChanged = true;
        }
    }

    /// <summary>
    /// Dynamically applies new calibration points from a browser request.
    /// </summary>
    public void ApplyNewCalibration(Point2f[] srcPoints, double realWidth, double realHeight)
    {
        var dstPoints = new Point2f[]
        {
            new(0f, 0f),
            new((float)realWidth, 0f),
            new((float)realWidth, (float)realHeight),
            new(0f, (float)realHeight)
        };

        var newH = Cv2.GetPerspectiveTransform(srcPoints, dstPoints);

        lock (_calibLock)
        {
            _currentHomography?.Dispose();
            _currentHomography = newH;
            _realWidth = realWidth;
            _realHeight = realHeight;
            _speedEstimator?.UpdateHomography(newH);
        }

        // Save to file for persistence
        var calibrator = new Calibrator();
        var calData = calibrator.CreateCalibrationData(srcPoints, realWidth, realHeight, newH, _videoPath, "Updated from Web Dashboard");
        calibrator.Save(_calibPath, calData);
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 1. Initialise Calibration
        var calibrator = new Calibrator();
        if (File.Exists(_calibPath))
        {
            var calData = calibrator.Load(_calibPath);
            _currentHomography = Calibrator.HomographyFromData(calData);
            _realWidth = calData.RealWorldWidthMetres;
            _realHeight = calData.RealWorldHeightMetres;
        }
        else
        {
            _currentHomography = calibrator.Calibrate(_videoPath, 25.0, 250.0);
            _realWidth = 25.0;
            _realHeight = 250.0;
        }

        // 2. Initialise Detector
        using var detector = new Detector(_modelPath, Config.InputSize);
        var tracker = new Tracker();
        var visualizer = new Visualizer();

        var frame = new Mat();

        _telemetry.SourceName = _videoPath;
        _telemetry.SourceMode = "file";
        _telemetry.InferenceEngine = detector.BackendName;
        _telemetry.IsGpuAccelerated = detector.IsGpuAccelerated;

        while (!stoppingToken.IsCancellationRequested)
        {
            if (_sourceChanged)
            {
                lock (_sourceLock)
                {
                    _videoPath = _pendingSource;
                    _sourceChanged = false;
                }
                tracker.Reset();
                _notifiedViolationTracks.Clear();
                _telemetry.Reset();
                _telemetry.SourceName = _videoPath;
                _telemetry.SourceMode = _videoPath.StartsWith("camera:") ? "camera" : "file";
            }

            // Open video reader
            VideoReader reader;
            if (_videoPath.StartsWith("camera:"))
            {
                int deviceIndex = 0;
                if (int.TryParse(_videoPath.Substring("camera:".Length), out int idx))
                {
                    deviceIndex = idx;
                }
                reader = VideoReader.OpenCamera(deviceIndex);
            }
            else
            {
                reader = VideoReader.Open(_videoPath);
            }

            using (reader)
            {
                _telemetry.Fps = reader.Fps;
                _telemetry.TotalFrames = reader.TotalFrames;

                lock (_calibLock)
                {
                    _speedEstimator = new SpeedEstimator(_currentHomography!, reader.Fps, reader.Height);
                }

                int frameNumber = 0;
                var stopwatch = System.Diagnostics.Stopwatch.StartNew();
                int fpsCounter = 0;

                while (!stoppingToken.IsCancellationRequested && reader.Read(frame) && !frame.Empty())
                {
                    if (_sourceChanged)
                        break;

                    // Run YOLOv8 detection at a lower cadence; the tracker predicts
                    // between detector frames to keep IDs and motion continuous.
                    var detections = frameNumber % Config.DetectionIntervalFrames == 0
                        ? detector.Detect(frame)
                        : new List<Detection>();

                    // Update IoU tracker
                    var tracks = tracker.Update(detections, frameNumber);

                    // Speed limit from telemetry
                    double currentLimit = _telemetry.SpeedLimitKmh;

                    // Estimate speed
                    lock (_calibLock)
                    {
                        foreach (var track in tracks)
                        {
                            if (track.MissedFrames == 0)
                                _speedEstimator.Estimate(track, currentLimit);

                            // If violation and not yet recorded for this vehicle
                            if (track.IsViolation && !_notifiedViolationTracks.Contains(track.TrackId))
                            {
                                _notifiedViolationTracks.Add(track.TrackId);
                                _telemetry.TotalViolations++;
                                _telemetry.RecordViolation(
                                    frameNumber,
                                    track.TrackId,
                                    track.ClassName,
                                    track.SpeedKmh,
                                    currentLimit
                                );
                            }
                        }
                    }

                    // Clean up old tracked violation IDs
                    if (_notifiedViolationTracks.Count > 200)
                    {
                        _notifiedViolationTracks.Clear();
                    }

                    // Update Telemetry metrics
                    _telemetry.CurrentFrame = frameNumber;
                    _telemetry.ActiveVehicles = tracks.Count;
                    _telemetry.TotalVehicles = tracker.TotalVehiclesCounted;

                    var movingTracks = tracks.Where(t => t.SpeedKmh > 0).ToList();
                    if (movingTracks.Count > 0)
                    {
                        _telemetry.AverageSpeedKmh = movingTracks.Average(t => t.SpeedKmh);
                    }

                    if (_telemetry.TotalVehicles > 0)
                    {
                        _telemetry.ViolationPercentage = (double)_telemetry.TotalViolations / _telemetry.TotalVehicles * 100.0;
                    }

                    // Calculate real FPS
                    fpsCounter++;
                    if (stopwatch.ElapsedMilliseconds >= 1000)
                    {
                        _telemetry.Fps = fpsCounter * 1000.0 / stopwatch.ElapsedMilliseconds;
                        fpsCounter = 0;
                        stopwatch.Restart();
                    }

                    // Draw computer vision visual overlays
                    lock (_calibLock)
                    {
                        visualizer.DrawOverlay(frame, tracks, currentLimit);
                        visualizer.DrawMinimap(frame, tracks, _currentHomography!, _realWidth, _realHeight);
                        visualizer.DrawStats(frame, tracker.TotalVehiclesCounted, _telemetry.AverageSpeedKmh, _telemetry.ViolationPercentage);
                    }

                    // Broadcast frame to web clients
                    _broadcaster.Broadcast(frame);

                    frameNumber++;

                }
            }

            if (_videoPath.StartsWith("camera:"))
            {
                continue;
            }

            // Loop video if stopped at EOF
            tracker.Reset();
            _notifiedViolationTracks.Clear();
            _telemetry.Reset();
            _telemetry.NeedsCalibration = false;
            _telemetry.SourceName = _videoPath;
            _telemetry.SourceMode = "file";
            await Task.Delay(500, stoppingToken);
        }
    }
}

using OpenCvSharp;

namespace CitizenRadar;

/// <summary>
/// All tunable constants for the CitizenRadar pipeline.
/// Centralised here so every team member references the same values
/// and tuning happens in one place during integration.
/// </summary>
public static class Config
{
    // ── Speed Estimation ──────────────────────────────────────────────
    /// <summary>Speed limit in km/h. Vehicles exceeding this are flagged as violations.</summary>
    public const double SpeedLimitKmh = 30.0;

    /// <summary>
    /// Number of frames used in the sliding window for speed calculation.
    /// At 30fps this gives a 1-second measurement window, long enough to
    /// average out bounding-box jitter while still being responsive.
    /// </summary>
    public const int SpeedWindowFrames = 30;

    /// <summary>
    /// Minimum number of position history entries before we display a speed value.
    /// At 30fps this equals 0.5 seconds of tracking — enough for a stable reading.
    /// </summary>
    public const int MinFramesBeforeSpeed = 15;

    /// <summary>Whether to apply Exponential Moving Average smoothing to speed values.</summary>
    public const bool EnableSmoothing = true;

    /// <summary>EMA smoothing factor (0–1). Lower = heavier smoothing. 0.3 works well with a 30-frame window.</summary>
    public const double SmoothingAlpha = 0.3;

    /// <summary>Maximum physically plausible speed in km/h. Values above this are rejected as perspective/tracking outliers.</summary>
    public const double MaxPlausibleSpeedKmh = 180.0;

    /// <summary>
    /// Fraction of frame height (from the top) where detections are too close to the
    /// perspective vanishing point for reliable speed measurement. Detections with
    /// bottom-center in the top 40% of the frame are ignored for speed computation
    /// (their track keeps its last stable speed instead of computing a noisy one).
    /// </summary>
    public const double HorizonGateRatio = 0.40;

    // ── Detection ─────────────────────────────────────────────────────
    /// <summary>Minimum confidence score to keep a detection.</summary>
    public const float ConfidenceThreshold = 0.25f;

    /// <summary>IoU threshold for Non-Maximum Suppression.</summary>
    public const float NmsThreshold = 0.45f;

    /// <summary>Input image size for YOLOv8 inference (square).</summary>
    public const int InputSize = 640;

    /// <summary>Run the detector on every Nth frame; the tracker predicts between detector frames.</summary>
    public const int DetectionIntervalFrames = 2;

    /// <summary>Maximum rate sent to browser clients; processing is not limited by this.</summary>
    public const int StreamMaxFps = 15;

    /// <summary>
    /// COCO class IDs for vehicles.
    /// car=2, motorcycle=3, bus=5, truck=7
    /// </summary>
    public static readonly int[] VehicleClassIds = { 2, 3, 5, 7 };

    // ── Tracking ──────────────────────────────────────────────────────
    /// <summary>Minimum IoU to consider a detection as matching an existing track.</summary>
    public const float IouThreshold = 0.3f;

    /// <summary>Maximum centre displacement allowed during association, in pixels.</summary>
    public const float MaxTrackCenterDistancePx = 260f;

    /// <summary>
    /// Number of consecutive frames a track can go unmatched before being deregistered.
    /// </summary>
    public const int TrackMaxMissedFrames = 30;

    // ── Visualization ─────────────────────────────────────────────────
    /// <summary>Bounding box colour for vehicles under the speed limit (BGR: green).</summary>
    public static readonly Scalar ColorSafe = new(0, 200, 0);

    /// <summary>Bounding box colour for vehicles exceeding the speed limit (BGR: red).</summary>
    public static readonly Scalar ColorViolation = new(0, 0, 255);

    /// <summary>Text colour for labels (BGR: white).</summary>
    public static readonly Scalar ColorText = new(255, 255, 255);

    /// <summary>Background colour for label backgrounds (BGR: dark).</summary>
    public static readonly Scalar ColorLabelBg = new(40, 40, 40);

    // ── Paths ─────────────────────────────────────────────────────────
    /// <summary>Default path to the ONNX model file.</summary>
    public const string DefaultModelPath = "models/yolov8n.onnx";

    /// <summary>Default path to save/load calibration data.</summary>
    public const string DefaultCalibrationPath = "data/calibration.json";

    /// <summary>Default path for the CSV speed log.</summary>
    public const string DefaultCsvPath = "output/speed_log.csv";

    /// <summary>Default path for the annotated output video.</summary>
    public const string DefaultOutputVideoPath = "output/annotated_output.mp4";
}

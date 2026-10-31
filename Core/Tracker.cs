using OpenCvSharp;
using CitizenRadar.Models;

namespace CitizenRadar.Core;

/// <summary>
/// Lightweight constant-velocity multi-object tracker.
/// Detector results are associated against a predicted bounding box instead of
/// the previous box, which keeps IDs stable when detection is intentionally
/// skipped for performance or a vehicle moves quickly between frames.
/// </summary>
public class Tracker
{
    private readonly Dictionary<int, Track> _activeTracks = new();
    private int _nextId = 1;

    public List<Track> Update(List<Detection> detections, int frameNumber)
    {
        foreach (var track in _activeTracks.Values)
        {
            if (track.MissedFrames == 0)
                continue;

            track.LastBBox = PredictBox(track, frameNumber);
        }

        var pairs = new List<(int TrackId, int DetectionIndex, float Score)>();
        foreach (var track in _activeTracks.Values)
        {
            for (int d = 0; d < detections.Count; d++)
            {
                var detection = detections[d];
                float iou = ComputeIoU(track.LastBBox, detection.BBox);
                float distance = CenterDistance(track.LastBBox, detection.BBox);
                if (iou < 0.10f && distance > Config.MaxTrackCenterDistancePx)
                    continue;

                float distanceScore = 1f - Math.Clamp(distance / Config.MaxTrackCenterDistancePx, 0f, 1f);
                float classScore = track.ClassId == detection.ClassId ? 1f : 0.55f;
                pairs.Add((track.TrackId, d, (iou * 0.55f) + (distanceScore * 0.35f) + (classScore * 0.10f)));
            }
        }

        pairs.Sort((a, b) => b.Score.CompareTo(a.Score));
        var matchedTracks = new HashSet<int>();
        var matchedDetections = new HashSet<int>();

        foreach (var (trackId, detectionIndex, _) in pairs)
        {
            if (!matchedTracks.Add(trackId) || !matchedDetections.Add(detectionIndex))
                continue;

            var track = _activeTracks[trackId];
            var detection = detections[detectionIndex];
            int frameDelta = Math.Max(1, frameNumber - track.LastObservedFrame);
            var previousCenter = Center(track.LastObservedBBox);
            var nextCenter = Center(detection.BBox);
            var measuredVelocity = new Point2f(
                (nextCenter.X - previousCenter.X) / frameDelta,
                (nextCenter.Y - previousCenter.Y) / frameDelta);

            track.Velocity = new Point2f(
                track.Velocity.X * 0.55f + measuredVelocity.X * 0.45f,
                track.Velocity.Y * 0.55f + measuredVelocity.Y * 0.45f);
            track.LastBBox = detection.BBox;
            track.LastObservedBBox = detection.BBox;
            track.LastObservedFrame = frameNumber;
            track.MissedFrames = 0;
            track.HitStreak++;
            track.TotalHits++;
            track.ClassId = detection.ClassId;
            AddHistory(track, frameNumber, detection.BottomCenter);
        }

        foreach (var track in _activeTracks.Values.ToList())
        {
            if (matchedTracks.Contains(track.TrackId))
                continue;

            track.MissedFrames++;
            track.HitStreak = 0;
            track.LastBBox = PredictBox(track, frameNumber);

            if (track.MissedFrames <= Config.TrackMaxMissedFrames)
            {
                AddHistory(track, frameNumber, BottomCenter(track.LastBBox));
            }
            else
            {
                _activeTracks.Remove(track.TrackId);
            }
        }

        for (int d = 0; d < detections.Count; d++)
        {
            if (matchedDetections.Contains(d))
                continue;

            var detection = detections[d];
            var track = new Track(_nextId++, detection.ClassId, detection.BBox, frameNumber, detection.BottomCenter);
            _activeTracks[track.TrackId] = track;
        }

        return _activeTracks.Values.ToList();
    }

    public int TotalVehiclesCounted => _nextId - 1;

    public void Reset()
    {
        _activeTracks.Clear();
        _nextId = 1;
    }

    private static void AddHistory(Track track, int frameNumber, Point2f position)
    {
        if (track.PositionHistory.Count > 0 && track.PositionHistory[^1].FrameNumber == frameNumber)
            track.PositionHistory[^1] = (frameNumber, position);
        else
            track.PositionHistory.Add((frameNumber, position));

        if (track.PositionHistory.Count > 60)
            track.PositionHistory.RemoveAt(0);
    }

    private static Rect2f PredictBox(Track track, int frameNumber)
    {
        int delta = Math.Max(1, frameNumber - track.LastObservedFrame);
        return new Rect2f(
            track.LastObservedBBox.X + track.Velocity.X * delta,
            track.LastObservedBBox.Y + track.Velocity.Y * delta,
            track.LastObservedBBox.Width,
            track.LastObservedBBox.Height);
    }

    private static Point2f Center(Rect2f box) => new(box.X + box.Width / 2f, box.Y + box.Height / 2f);
    private static Point2f BottomCenter(Rect2f box) => new(box.X + box.Width / 2f, box.Y + box.Height);

    private static float CenterDistance(Rect2f a, Rect2f b)
    {
        var ca = Center(a);
        var cb = Center(b);
        return MathF.Sqrt(MathF.Pow(ca.X - cb.X, 2) + MathF.Pow(ca.Y - cb.Y, 2));
    }

    private static float ComputeIoU(Rect2f a, Rect2f b)
    {
        float x1 = Math.Max(a.X, b.X);
        float y1 = Math.Max(a.Y, b.Y);
        float x2 = Math.Min(a.X + a.Width, b.X + b.Width);
        float y2 = Math.Min(a.Y + a.Height, b.Y + b.Height);
        float interWidth = Math.Max(0, x2 - x1);
        float interHeight = Math.Max(0, y2 - y1);
        float interArea = interWidth * interHeight;
        if (interArea == 0) return 0f;
        float unionArea = a.Width * a.Height + b.Width * b.Height - interArea;
        return unionArea > 0 ? interArea / unionArea : 0f;
    }
}

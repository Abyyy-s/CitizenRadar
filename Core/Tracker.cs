using OpenCvSharp;
using CitizenRadar.Models;

namespace CitizenRadar.Core;

public class Tracker
{
    private readonly List<Track> _tracks = new();

    private int _nextTrackId = 1;

    private readonly float _iouThreshold;
    private readonly int _maxMissedFrames;
    private readonly float _maxAnchorDistance;

    // How long a lost vehicle should be remembered
    private readonly int _recoveryFrames = 30;

    public IReadOnlyList<Track> Tracks => _tracks;

    public Tracker(
        float iouThreshold = 0.15f,
        int maxMissedFrames = 12,
        float maxAnchorDistance = 180f)
    {
        _iouThreshold = iouThreshold;
        _maxMissedFrames = maxMissedFrames;
        _maxAnchorDistance = maxAnchorDistance;
    }

    public List<Track> Update(
        List<Detection> detections,
        int frameIndex)
    {
        // --------------------------------------------------
        // STEP 1: If there are no existing tracks
        // --------------------------------------------------

        if (_tracks.Count == 0)
        {
            foreach (var detection in detections)
            {
                CreateTrack(detection, frameIndex);
            }

            return _tracks.ToList();
        }

        // --------------------------------------------------
        // STEP 2: Create all possible track-detection matches
        // --------------------------------------------------

        var possibleMatches =
            new List<(int trackIndex, int detectionIndex, float score)>();

        for (int t = 0; t < _tracks.Count; t++)
        {
            var track = _tracks[t];

            // Do not normally match a very old lost track
            if (track.State == TrackState.Lost &&
                track.MissedFrames > _recoveryFrames)
            {
                continue;
            }

            Point2f predictedPosition =
                PredictPosition(track);

            float recoveryDistance =
                GetRecoveryDistance(track);

            for (int d = 0; d < detections.Count; d++)
            {
                var detection = detections[d];

                // Vehicle class must match
                if (track.ClassName != detection.ClassName)
                {
                    continue;
                }

                float iou =
                    CalculateIoU(
                        track.BoundingBox,
                        detection.BoundingBox
                    );

                float distance =
                    CalculateDistance(
                        predictedPosition,
                        detection.AnchorPoint
                    );

                // Size / shape similarity
                float sizeScore =
                    CalculateSizeScore(
                        track.BoundingBox,
                        detection.BoundingBox
                    );

                // Direction similarity
                float directionScore =
                    CalculateDirectionScore(
                        track,
                        detection.AnchorPoint
                    );

                // --------------------------------------------------
                // Matching gate
                // --------------------------------------------------

                bool validMatch =
                    iou >= _iouThreshold ||
                    distance <= recoveryDistance;

                if (!validMatch)
                {
                    continue;
                }

                // Distance score
                float distanceScore =
                    Math.Max(
                        0f,
                        1f -
                        distance / recoveryDistance
                    );

                // --------------------------------------------------
                // Final matching score
                // --------------------------------------------------

                float score =
                    (iou * 0.40f) +
                    (distanceScore * 0.25f) +
                    (directionScore * 0.15f) +
                    (sizeScore * 0.20f);

                // Lost tracks get a small penalty.
                // This prevents an old track from stealing
                // a detection from a currently active vehicle.
                if (track.State == TrackState.Lost)
                {
                    score *= 0.90f;
                }

                possibleMatches.Add(
                    (
                        t,
                        d,
                        score
                    )
                );
            }
        }

        // --------------------------------------------------
        // STEP 3: Sort matches from best to worst
        // --------------------------------------------------

        possibleMatches =
            possibleMatches
                .OrderByDescending(x => x.score)
                .ToList();

        var matchedTracks =
            new HashSet<int>();

        var matchedDetections =
            new HashSet<int>();

        // --------------------------------------------------
        // STEP 4: Perform global matching
        // --------------------------------------------------

        foreach (var match in possibleMatches)
        {
            if (matchedTracks.Contains(match.trackIndex))
            {
                continue;
            }

            if (matchedDetections.Contains(match.detectionIndex))
            {
                continue;
            }

            var track =
                _tracks[match.trackIndex];

            var detection =
                detections[match.detectionIndex];

            track.Update(
                detection,
                frameIndex
            );

            matchedTracks.Add(
                match.trackIndex
            );

            matchedDetections.Add(
                match.detectionIndex
            );
        }

        // --------------------------------------------------
        // STEP 5: Mark unmatched tracks as missed
        // --------------------------------------------------

        for (int i = 0; i < _tracks.Count; i++)
        {
            if (!matchedTracks.Contains(i))
            {
                _tracks[i].MarkMissed(
                    _maxMissedFrames
                );
            }
        }

        // --------------------------------------------------
        // STEP 6: Try to recover unmatched detections
        // using old lost tracks
        // --------------------------------------------------

        for (int d = 0; d < detections.Count; d++)
        {
            if (matchedDetections.Contains(d))
            {
                continue;
            }

            var detection =
                detections[d];

            int bestLostTrackIndex = -1;

            float bestRecoveryScore = 0f;

            for (int t = 0; t < _tracks.Count; t++)
            {
                if (matchedTracks.Contains(t))
                {
                    continue;
                }

                var track =
                    _tracks[t];

                if (track.State != TrackState.Lost)
                {
                    continue;
                }

                if (track.MissedFrames >
                    _recoveryFrames)
                {
                    continue;
                }

                if (track.ClassName !=
                    detection.ClassName)
                {
                    continue;
                }

                Point2f predictedPosition =
                    PredictPosition(track);

                float distance =
                    CalculateDistance(
                        predictedPosition,
                        detection.AnchorPoint
                    );

                float recoveryDistance =
                    GetRecoveryDistance(track);

                if (distance > recoveryDistance)
                {
                    continue;
                }

                float distanceScore =
                    Math.Max(
                        0f,
                        1f -
                        distance / recoveryDistance
                    );

                float sizeScore =
                    CalculateSizeScore(
                        track.BoundingBox,
                        detection.BoundingBox
                    );

                float score =
                    (distanceScore * 0.60f) +
                    (sizeScore * 0.40f);

                if (score > bestRecoveryScore)
                {
                    bestRecoveryScore =
                        score;

                    bestLostTrackIndex =
                        t;
                }
            }

            // --------------------------------------------------
            // Recover old ID
            // --------------------------------------------------

            if (bestLostTrackIndex >= 0 &&
                bestRecoveryScore >= 0.55f)
            {
                var recoveredTrack =
                    _tracks[bestLostTrackIndex];

                recoveredTrack.Update(
                    detection,
                    frameIndex
                );

                matchedTracks.Add(
                    bestLostTrackIndex
                );

                matchedDetections.Add(
                    d
                );
            }
        }

        // --------------------------------------------------
        // STEP 7: Create new tracks for detections
        // that could not be matched or recovered
        // --------------------------------------------------

        for (int d = 0; d < detections.Count; d++)
        {
            if (matchedDetections.Contains(d))
            {
                continue;
            }

            CreateTrack(
                detections[d],
                frameIndex
            );
        }

        // --------------------------------------------------
        // STEP 8: Remove tracks that have been lost
        // for too long
        // --------------------------------------------------

        RemoveLostTracks();

        return _tracks.ToList();
    }

    // ======================================================
    // Recovery distance
    // ======================================================

    private float GetRecoveryDistance(
        Track track)
    {
        float distance =
            _maxAnchorDistance +
            (track.MissedFrames * 20f);

        return Math.Min(
            distance,
            300f
        );
    }

    // ======================================================
    // Predict vehicle position
    // ======================================================

    private Point2f PredictPosition(
        Track track)
    {
        if (track.History.Count < 2)
        {
            return track.AnchorPoint;
        }

        Point2f previous =
            track.History[
                track.History.Count - 2
            ];

        Point2f current =
            track.History[
                track.History.Count - 1
            ];

        float velocityX =
            current.X - previous.X;

        float velocityY =
            current.Y - previous.Y;

        float framesAhead =
            track.MissedFrames + 1;

        return new Point2f(
            current.X +
            velocityX * framesAhead,

            current.Y +
            velocityY * framesAhead
        );
    }

    // ======================================================
    // Size and shape similarity
    // ======================================================

    private static float CalculateSizeScore(
        Rect2f trackBox,
        Rect2f detectionBox)
    {
        if (
            trackBox.Width <= 0 ||
            trackBox.Height <= 0 ||
            detectionBox.Width <= 0 ||
            detectionBox.Height <= 0
        )
        {
            return 0f;
        }

        float widthRatio =
            Math.Min(
                trackBox.Width /
                detectionBox.Width,

                detectionBox.Width /
                trackBox.Width
            );

        float heightRatio =
            Math.Min(
                trackBox.Height /
                detectionBox.Height,

                detectionBox.Height /
                trackBox.Height
            );

        float trackAspect =
            trackBox.Width /
            trackBox.Height;

        float detectionAspect =
            detectionBox.Width /
            detectionBox.Height;

        float aspectRatio =
            Math.Min(
                trackAspect /
                detectionAspect,

                detectionAspect /
                trackAspect
            );

        return
            (widthRatio * 0.40f) +
            (heightRatio * 0.40f) +
            (aspectRatio * 0.20f);
    }

    // ======================================================
    // Direction similarity
    // ======================================================

    private static float CalculateDirectionScore(
        Track track,
        Point2f detectionPoint)
    {
        if (track.History.Count < 2)
        {
            return 0.5f;
        }

        Point2f previous =
            track.History[
                track.History.Count - 2
            ];

        Point2f current =
            track.History[
                track.History.Count - 1
            ];

        float vx1 =
            current.X - previous.X;

        float vy1 =
            current.Y - previous.Y;

        float vx2 =
            detectionPoint.X - current.X;

        float vy2 =
            detectionPoint.Y - current.Y;

        float magnitude1 =
            MathF.Sqrt(
                vx1 * vx1 +
                vy1 * vy1
            );

        float magnitude2 =
            MathF.Sqrt(
                vx2 * vx2 +
                vy2 * vy2
            );

        if (
            magnitude1 < 0.001f ||
            magnitude2 < 0.001f
        )
        {
            return 0.5f;
        }

        float cosine =
            (
                vx1 * vx2 +
                vy1 * vy2
            ) /
            (
                magnitude1 *
                magnitude2
            );

        return
            Math.Clamp(
                (cosine + 1f) / 2f,
                0f,
                1f
            );
    }

    // ======================================================
    // Create new track
    // ======================================================

    private void CreateTrack(
        Detection detection,
        int frameIndex)
    {
        var track =
            new Track(
                _nextTrackId++,
                detection,
                frameIndex
            );

        _tracks.Add(track);
    }

    // ======================================================
    // Remove very old lost tracks
    // ======================================================

    private void RemoveLostTracks()
    {
        _tracks.RemoveAll(
            track =>
                track.State == TrackState.Lost &&
                track.MissedFrames >
                _recoveryFrames
        );
    }

    // ======================================================
    // IoU calculation
    // ======================================================

    private static float CalculateIoU(
        Rect2f a,
        Rect2f b)
    {
        float x1 =
            Math.Max(a.X, b.X);

        float y1 =
            Math.Max(a.Y, b.Y);

        float x2 =
            Math.Min(
                a.X + a.Width,
                b.X + b.Width
            );

        float y2 =
            Math.Min(
                a.Y + a.Height,
                b.Y + b.Height
            );

        float intersectionWidth =
            Math.Max(
                0f,
                x2 - x1
            );

        float intersectionHeight =
            Math.Max(
                0f,
                y2 - y1
            );

        float intersectionArea =
            intersectionWidth *
            intersectionHeight;

        float areaA =
            a.Width *
            a.Height;

        float areaB =
            b.Width *
            b.Height;

        float unionArea =
            areaA +
            areaB -
            intersectionArea;

        if (unionArea <= 0f)
        {
            return 0f;
        }

        return intersectionArea /
               unionArea;
    }

    // ======================================================
    // Distance calculation
    // ======================================================

    private static float CalculateDistance(
        Point2f a,
        Point2f b)
    {
        float dx =
            a.X - b.X;

        float dy =
            a.Y - b.Y;

        return MathF.Sqrt(
            dx * dx +
            dy * dy
        );
    }
}
using OpenCvSharp;

namespace CitizenRadar.Models;

public enum TrackState
{
    Tentative,
    Confirmed,
    Active,
    Coasting,
    Lost
}

public class Track
{
    public int Id { get; }

    public string ClassName { get; private set; }

    public int ClassId { get; private set; }

    public float Confidence { get; private set; }

    public Rect2f BoundingBox { get; private set; }

    public Point2f AnchorPoint { get; private set; }

    public List<Point2f> History { get; } = new();

    public int MissedFrames { get; private set; }

    public int HitCount { get; private set; }

    public TrackState State { get; private set; }
    public int MaxHistoryLength { get; set; } = 30;

    public Track(
        int id,
        Detection detection,
        int frameIndex)
    {
        Id = id;

        ClassName = detection.ClassName;
        ClassId = detection.ClassId;
        Confidence = detection.Confidence;
        BoundingBox = detection.BoundingBox;

        AnchorPoint = new Point2f(
            detection.BoundingBox.X +
                detection.BoundingBox.Width / 2,

            detection.BoundingBox.Y +
                detection.BoundingBox.Height
        );
        History = new List<Point2f>();

        

        History.Add(AnchorPoint);
        if (History.Count > MaxHistoryLength)
{
    History.RemoveAt(0);
}

        MissedFrames = 0;

        HitCount = 1;

        State = TrackState.Tentative;
    }

    public void Update(
        Detection detection,
        int frameIndex)
    {
        ClassName = detection.ClassName;
        ClassId = detection.ClassId;
        Confidence = detection.Confidence;
        BoundingBox = detection.BoundingBox;

        AnchorPoint = new Point2f(
            detection.BoundingBox.X +
                detection.BoundingBox.Width / 2,

            detection.BoundingBox.Y +
                detection.BoundingBox.Height
        );

        History.Add(AnchorPoint);

// Keep only the most recent history points.
if (History.Count > MaxHistoryLength)
{
    History.RemoveAt(0);
}

MissedFrames = 0;

        HitCount++;

        // First successful association after
        // a temporary disappearance.
        if (State == TrackState.Coasting)
        {
            State = TrackState.Active;
        }
        else if (HitCount >= 2)
        {
            State = TrackState.Active;
        }
        else
        {
            State = TrackState.Tentative;
        }
    }

    public void MarkMissed(int maxMissedFrames)
    {
        MissedFrames++;

        if (MissedFrames > maxMissedFrames)
        {
            State = TrackState.Lost;
        }
        else
        {
            State = TrackState.Coasting;
        }
    }
}
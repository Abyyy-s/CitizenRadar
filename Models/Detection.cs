using OpenCvSharp;

namespace CitizenRadar.Models;

public class Detection
{
    public int ClassId { get; }

    public string ClassName { get; }

    public float Confidence { get; }

    public Rect2f BoundingBox { get; }

    public Point2f AnchorPoint { get; }

    public Detection(
        int classId,
        string className,
        float confidence,
        Rect2f boundingBox)
    {
        ClassId = classId;
        ClassName = className;
        Confidence = confidence;
        BoundingBox = boundingBox;

        AnchorPoint = new Point2f(
            boundingBox.X + boundingBox.Width / 2f,
            boundingBox.Y + boundingBox.Height
        );
    }
}
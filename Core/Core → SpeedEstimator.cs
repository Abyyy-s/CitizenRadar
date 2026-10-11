using OpenCvSharp;

namespace CitizenRadar.Core;

public class SpeedEstimator
{
    private readonly float _fps;

    public SpeedEstimator(float fps)
    {
        if (fps <= 0)
        {
            throw new ArgumentException(
                "FPS must be greater than zero."
            );
        }

        _fps = fps;
    }

    public float CalculateSpeed(
        Point2f previousPosition,
        Point2f currentPosition,
        int frameDifference)
    {
        if (frameDifference <= 0)
        {
            return 0f;
        }

        // Calculate distance in metres
        float dx =
            currentPosition.X -
            previousPosition.X;

        float dy =
            currentPosition.Y -
            previousPosition.Y;

        float distance =
            MathF.Sqrt(
                dx * dx +
                dy * dy
            );

        // Convert frame difference to seconds
        float time =
            frameDifference / _fps;

        if (time <= 0)
        {
            return 0f;
        }

        // Speed in metres per second
        float speedMetersPerSecond =
            distance / time;

        // Convert m/s to km/h
        float speedKmPerHour =
            speedMetersPerSecond * 3.6f;

        return speedKmPerHour;
    }
}
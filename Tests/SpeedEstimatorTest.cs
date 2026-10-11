using CitizenRadar.Core;
using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class SpeedEstimatorTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine(" CitizenRadar Speed Test");
        Console.WriteLine("=================================");

        // Video information
        float fps = 30f;

        var estimator =
            new SpeedEstimator(fps);

        // Vehicle movement in real-world metres
        Point2f previousPosition =
            new Point2f(5, 10);

        Point2f currentPosition =
            new Point2f(5, 16);

        // 30 frames passed
        int frameDifference = 30;

        float speed =
            estimator.CalculateSpeed(
                previousPosition,
                currentPosition,
                frameDifference
            );

        Console.WriteLine();
        Console.WriteLine(
            $"Previous position: ({previousPosition.X}, {previousPosition.Y}) m"
        );

        Console.WriteLine(
            $"Current position: ({currentPosition.X}, {currentPosition.Y}) m"
        );

        Console.WriteLine(
            $"Frames: {frameDifference}"
        );

        Console.WriteLine(
            $"FPS: {fps}"
        );

        Console.WriteLine();
        Console.WriteLine(
            $"Calculated speed: {speed:F2} km/h"
        );

        Console.WriteLine();
        Console.WriteLine("Speed test completed.");
        Console.WriteLine("=================================");
    }
}
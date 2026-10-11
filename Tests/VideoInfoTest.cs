using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class VideoInfoTest
{
    public static void Run()
    {
        string videoPath = Path.Combine(
            "TestData",
            "traffic.mp4"
        );

        if (!File.Exists(videoPath))
        {
            Console.WriteLine("Video file not found!");
            return;
        }

        using var capture = new VideoCapture(videoPath);

        if (!capture.IsOpened())
        {
            Console.WriteLine("Could not open video!");
            return;
        }

        Console.WriteLine("=== CitizenRadar Video Information ===");
        Console.WriteLine($"Width: {capture.FrameWidth}");
        Console.WriteLine($"Height: {capture.FrameHeight}");
        Console.WriteLine($"FPS: {capture.Fps:F2}");
        Console.WriteLine($"Total frames: {capture.FrameCount}");

        if (capture.Fps > 0)
        {
            double duration = capture.FrameCount / capture.Fps;
            Console.WriteLine($"Duration: {duration:F2} seconds");
        }
    }
}
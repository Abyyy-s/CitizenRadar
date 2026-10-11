using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class VideoTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine("   CitizenRadar Video Test");
        Console.WriteLine("=================================");

        string videoPath = Path.Combine(
            "TestData",
            "traffic.mp4"
        );

        if (!File.Exists(videoPath))
        {
            Console.WriteLine("❌ Video file not found.");
            Console.WriteLine($"Expected: {videoPath}");
            return;
        }

        using var capture = new VideoCapture(videoPath);

        if (!capture.IsOpened())
        {
            Console.WriteLine("❌ Could not open video.");
            return;
        }

        double fps = capture.Fps;
        int width = (int)capture.FrameWidth;
        int height = (int)capture.FrameHeight;
        int totalFrames = (int)capture.FrameCount;

        Console.WriteLine();
        Console.WriteLine($"Video width  : {width}");
        Console.WriteLine($"Video height : {height}");
        Console.WriteLine($"FPS          : {fps:F2}");
        Console.WriteLine($"Total frames : {totalFrames}");

        using var frame = new Mat();

        int frameIndex = 0;

        while (true)
        {
            bool success = capture.Read(frame);

            if (!success || frame.Empty())
            {
                break;
            }

            frameIndex++;

            Console.WriteLine(
                $"Reading frame: {frameIndex}/{totalFrames}"
            );
        }

        Console.WriteLine();
        Console.WriteLine("=================================");
        Console.WriteLine($"✅ Video read successfully!");
        Console.WriteLine($"Frames processed: {frameIndex}");
        Console.WriteLine("=================================");
    }
}
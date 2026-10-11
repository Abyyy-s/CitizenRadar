using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class FrameCaptureTest
{
    public static void Run()
    {
        string videoPath = Path.Combine(
            "TestData", "traffic.mp4"
        );

        string imagePath = Path.Combine(
            "TestData", "calibration_frame.jpg"
        );

        using var capture = new VideoCapture(videoPath);
        using var frame = new Mat();

        // Extract a frame at approximately 2 seconds
        capture.Set(
            VideoCaptureProperties.PosMsec,
            2000
        );

        if (!capture.Read(frame) || frame.Empty())
        {
            Console.WriteLine("Could not extract frame.");
            return;
        }

        Cv2.ImWrite(imagePath, frame);

        Console.WriteLine(
            $"Frame saved to: {imagePath}"
        );
        Console.WriteLine(
            $"Frame size: {frame.Width} x {frame.Height}"
        );
    }
}
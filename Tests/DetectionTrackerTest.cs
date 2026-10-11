using OpenCvSharp;
using CitizenRadar.Core;

namespace CitizenRadar.Tests;

public static class DetectionTrackerTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine(" CitizenRadar Detection + Tracker");
        Console.WriteLine("=================================");

        string modelPath = Path.Combine(
            "ModelFiles",
            "yolov8n.onnx"
        );

        string imagePath = Path.Combine(
            "TestData",
            "traffic.jpg"
        );

        if (!File.Exists(modelPath))
        {
            Console.WriteLine("❌ YOLO model not found.");
            return;
        }

        if (!File.Exists(imagePath))
        {
            Console.WriteLine("❌ Test image not found.");
            return;
        }

        using var image = Cv2.ImRead(
            imagePath,
            ImreadModes.Color
        );

        if (image.Empty())
        {
            Console.WriteLine("❌ Could not read image.");
            return;
        }

        // Create detector
        var detector = new Detector(modelPath);

        // Create tracker
        var tracker = new CitizenRadar.Core.Tracker(
         iouThreshold: 0.3f,
         maxMissedFrames: 10
);

        Console.WriteLine();
        Console.WriteLine("Running detection...");

        // Detect vehicles
        var detections = detector.Detect(image);

        Console.WriteLine(
            $"Vehicles detected: {detections.Count}"
        );

        // Send detections to tracker
        var tracks = tracker.Update(detections, 1);

        Console.WriteLine();
        Console.WriteLine("Tracker Results");
        Console.WriteLine("----------------");

        foreach (var track in tracks)
        {
            Console.WriteLine(
                $"ID: {track.Id} | " +
                $"Class: {track.ClassName} | " +
                $"State: {track.State} | " +
                $"Confidence: {track.Confidence:F2}"
            );
        }

        Console.WriteLine();
        Console.WriteLine("✅ Detection + Tracking completed!");
    }
}
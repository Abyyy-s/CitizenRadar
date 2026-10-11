using OpenCvSharp;
using CitizenRadar.Core;

namespace CitizenRadar.Tests;

public static class DetectionTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine("   CitizenRadar YOLO Detection");
        Console.WriteLine("=================================");

        string modelPath = Path.Combine(
            "ModelFiles",
            "yolov8n.onnx"
        );

        string imagePath = Path.Combine(
            "TestData",
            "traffic.jpg"
        );

        string outputPath = Path.Combine(
            "TestData",
            "detected_output.jpg"
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

        Console.WriteLine(
            $"Image size: {image.Width} x {image.Height}"
        );

        try
        {
            var detector = new Detector(modelPath);

            Console.WriteLine("Running YOLO detection...");

            var detections = detector.Detect(image);

            Console.WriteLine();
            Console.WriteLine("Detection Results");
            Console.WriteLine("-----------------");

            foreach (var detection in detections)
            {
                Console.WriteLine(
                    $"Class: {detection.ClassName} | " +
                    $"Confidence: {detection.Confidence:F2}"
                );

                // Convert Rect2f to Rect
                var box = new Rect(
                    (int)detection.BoundingBox.X,
                    (int)detection.BoundingBox.Y,
                    (int)detection.BoundingBox.Width,
                    (int)detection.BoundingBox.Height
                );

                // Draw bounding box
                Cv2.Rectangle(
                    image,
                    box,
                    Scalar.LimeGreen,
                    3
                );

                // Create label
                string label =
                    $"{detection.ClassName} " +
                    $"{detection.Confidence:P0}";

                // Draw label background
                Cv2.PutText(
                    image,
                    label,
                    new Point(box.X, Math.Max(25, box.Y - 10)),
                    HersheyFonts.HersheySimplex,
                    0.8,
                    Scalar.Red,
                    2
                );
            }

            // Save output image
            Cv2.ImWrite(
                outputPath,
                image
            );

            Console.WriteLine();
            Console.WriteLine(
                $"Total vehicles detected: {detections.Count}"
            );

            Console.WriteLine(
                $"✅ Output saved to: {outputPath}"
            );
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Detection failed.");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
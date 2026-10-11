using CitizenRadar.Core;

namespace CitizenRadar.Tests;

public static class ModelLoadTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine("   CitizenRadar YOLO Model Test");
        Console.WriteLine("=================================");

        string modelPath = Path.Combine(
            "ModelFiles",
            "yolov8n.onnx"
        );

        Console.WriteLine($"Model path: {modelPath}");

        // Check whether the file exists
        if (!File.Exists(modelPath))
        {
            Console.WriteLine("❌ Model file NOT found.");
            return;
        }

        Console.WriteLine("✅ Model file found.");
        Console.WriteLine("Loading YOLOv8 model...");

        try
        {
            var detector = new Detector(modelPath);

            Console.WriteLine("✅ YOLOv8 model loaded successfully!");
            Console.WriteLine("Detector is ready.");
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Failed to load YOLO model.");
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
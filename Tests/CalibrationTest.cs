
using OpenCvSharp;
using CitizenRadar.Core;
using CitizenRadar.Models;

namespace CitizenRadar.Tests;

public static class CalibrationTest
{
    public static void Run()
    {
        Console.WriteLine("=== CitizenRadar Calibration Test ===");

        var data = new CalibrationData();
        var calibrator = new Calibrator(data);

        for (int i = 0; i < data.ImagePoints.Length; i++)
        {
            Point2f pixel = data.ImagePoints[i];

            Point2f world = calibrator.PixelToWorld(pixel);

            Console.WriteLine(
                $"P{i + 1}: Pixel ({pixel.X:F1}, {pixel.Y:F1}) " +
                $"-> World ({world.X:F2}, {world.Y:F2}) metres"
            );
        }

        // Test the centre of the assumed world rectangle
        Point2f testPixel = new Point2f(638, 400);

        Point2f testWorld = calibrator.PixelToWorld(testPixel);

        Console.WriteLine();
        Console.WriteLine(
            $"Test pixel ({testPixel.X:F1}, {testPixel.Y:F1}) " +
            $"-> World ({testWorld.X:F2}, {testWorld.Y:F2}) metres"
        );

        Console.WriteLine("Calibration test completed.");
    }
}
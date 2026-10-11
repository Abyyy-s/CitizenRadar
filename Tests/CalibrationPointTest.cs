
using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class CalibrationPointTest
{
    public static void Run()
    {
        string imagePath = Path.Combine(
            "TestData",
            "calibration_frame.jpg"
        );

        using var image = Cv2.ImRead(imagePath);

        if (image.Empty())
        {
            Console.WriteLine("Could not load calibration_frame.jpg");
            return;
        }

        var points = new List<Point>();

        using var display = image.Clone();

        Cv2.NamedWindow(
            "Select 4 Road Points",
            WindowFlags.Normal
        );

        Cv2.SetMouseCallback(
            "Select 4 Road Points",
            (eventType, x, y, flags, userData) =>
            {
                if (eventType != MouseEventTypes.LButtonDown)
                    return;

                if (points.Count >= 4)
                    return;

                points.Add(new Point(x, y));

                Cv2.Circle(
                    display,
                    new Point(x, y),
                    6,
                    Scalar.LimeGreen,
                    -1
                );

                Cv2.PutText(
                    display,
                    $"P{points.Count}",
                    new Point(x + 8, y - 8),
                    HersheyFonts.HersheySimplex,
                    0.7,
                    Scalar.Yellow,
                    2
                );

                Cv2.ImShow(
                    "Select 4 Road Points",
                    display
                );

                Console.WriteLine(
                    $"P{points.Count} = ({x}, {y})"
                );

                if (points.Count == 4)
                {
                    Console.WriteLine();
                    Console.WriteLine("Four points selected:");
                    for (int i = 0; i < points.Count; i++)
                    {
                        Console.WriteLine(
                            $"P{i + 1}: ({points[i].X}, {points[i].Y})"
                        );
                    }

                    Console.WriteLine(
                        "Close the image window to finish."
                    );
                }
            }
        );

        Cv2.ImShow(
            "Select 4 Road Points",
            display
        );

        Cv2.WaitKey(0);
        Cv2.DestroyAllWindows();
    }
}
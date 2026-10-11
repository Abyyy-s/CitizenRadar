
using OpenCvSharp;

namespace CitizenRadar.Models;

public class CalibrationData
{
    public Point2f[] ImagePoints { get; set; } =
    {
        new Point2f(17, 218),       // P1
        new Point2f(1259, 325),     // P2
        new Point2f(1246, 577),     // P3
        new Point2f(38, 525)        // P4
    };

    public Point2f[] WorldPoints { get; set; } =
    {
        new Point2f(0, 0),          // P1
        new Point2f(20, 0),         // P2
        new Point2f(20, 10),        // P3
        new Point2f(0, 10)          // P4
    };
}
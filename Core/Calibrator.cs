using OpenCvSharp;
using CitizenRadar.Models;

namespace CitizenRadar.Core;

public class Calibrator
{
    private readonly Mat _homography;

    public CalibrationData Data { get; }

    public Calibrator(CalibrationData data)
    {
        if (data.ImagePoints.Length != 4 ||
            data.WorldPoints.Length != 4)
        {
            throw new ArgumentException(
                "Calibration requires exactly 4 image points and 4 world points."
            );
        }

        Data = data;

        _homography = Cv2.GetPerspectiveTransform(
            data.ImagePoints,
            data.WorldPoints
        );
    }

    public Point2f PixelToWorld(Point2f pixelPoint)
    {
        Point2f[] input =
        {
            pixelPoint
        };

        Point2f[] output =
            Cv2.PerspectiveTransform(
                input,
                _homography
            );

        return output[0];
    }
}

using System;
using System.Collections.Generic;
using System.IO;
using OpenCvSharp;
using CitizenRadar.Core;
using CitizenRadar.Models;

namespace CitizenRadar.Tests
{
    public static class VideoTrackingTest
    {
        public static void Run()
        {
            Console.WriteLine("=================================");
            Console.WriteLine(" CitizenRadar Video Tracking");
            Console.WriteLine("=================================");

            string modelPath = Path.Combine(
                "ModelFiles", "yolov8n.onnx");

            string videoPath = Path.Combine(
                "TestData", "traffic.mp4");

            string outputPath = Path.Combine(
                "TestData", "tracked_output.mp4");

            // 1. Check required files.
            if (!File.Exists(modelPath))
            {
                Console.WriteLine("YOLO model not found: " + modelPath);
                return;
            }

            if (!File.Exists(videoPath))
            {
                Console.WriteLine("Traffic video not found: " + videoPath);
                return;
            }

            // 2. Open input video.
            using var capture = new VideoCapture(videoPath);

            if (!capture.IsOpened())
            {
                Console.WriteLine("Could not open the traffic video.");
                return;
            }

            int width = (int)capture.FrameWidth;
            int height = (int)capture.FrameHeight;
            double fps = capture.Fps;

            if (fps <= 0)
            {
                Console.WriteLine("Invalid video FPS.");
                return;
            }

            Console.WriteLine($"Video: {width} x {height}");
            Console.WriteLine($"FPS: {fps:F2}");

            // 3. Create output video writer.
            using var writer = new VideoWriter(
                outputPath,
                FourCC.MP4V,
                fps,
                new Size(width, height));

            if (!writer.IsOpened())
            {
                Console.WriteLine("Could not create output video.");
                return;
            }

            // 4. Initialize detector.
            Console.WriteLine("Loading YOLO model...");
            var detector = new Detector(modelPath);

            // 5. Initialize tracker.
            var tracker = new CitizenRadar.Core.Tracker(
                iouThreshold: 0.15f,
                maxMissedFrames: 12,
                maxAnchorDistance: 180f);

            // 6. Initialize calibration.
            // Uses points from Models/CalibrationData.cs.
            var calibrationData = new CalibrationData();
            var calibrator = new Calibrator(calibrationData);

            // 7. Initialize speed estimator.
            var speedEstimator = new SpeedEstimator((float)fps);

            // Store motion data for each vehicle ID.
            var vehicleMotions =
                new Dictionary<int, VehicleMotion>();

            Console.WriteLine("Detector ready.");
            Console.WriteLine("Tracker ready.");
            Console.WriteLine("Calibration ready.");
            Console.WriteLine("Speed estimator ready.");
            Console.WriteLine("Processing video...");

            using var frame = new Mat();
            int frameIndex = 0;

            // 8. Process video frame by frame.
            while (true)
            {
                bool success = capture.Read(frame);

                if (!success || frame.Empty())
                    break;

                frameIndex++;

                // Detect vehicles.
                var detections = detector.Detect(frame);

                // Track detected vehicles.
                var tracks = tracker.Update(
                    detections, frameIndex);

                // 9. Estimate vehicle speeds.
                foreach (var track in tracks)
                {
                    // Skip missed detections.
                    if (track.MissedFrames > 0 ||
                        track.State == TrackState.Lost)
                    {
                        continue;
                    }

                    // Convert pixel position to world coordinates.
                    Point2f worldPosition =
                        calibrator.PixelToWorld(track.AnchorPoint);

                    // First observation of this vehicle.
                    if (!vehicleMotions.TryGetValue(
                        track.Id, out var motion))
                    {
                        vehicleMotions[track.Id] =
                            new VehicleMotion(
                                track.Id,
                                worldPosition,
                                frameIndex);

                        continue;
                    }

                    int frameDifference =
                        frameIndex - motion.PreviousFrame;

                    if (frameDifference <= 0)
                        continue;

                    // Avoid calculations across long gaps.
                    if (frameDifference > 15)
                    {
                        motion.Reset(worldPosition, frameIndex);
                        continue;
                    }

                    float speed = speedEstimator.CalculateSpeed(
                        motion.PreviousWorldPosition,
                        worldPosition,
                        frameDifference);

                    motion.Update(
                        worldPosition,
                        frameIndex,
                        speed);
                }

                // 10. Draw vehicle boxes and labels.
                foreach (var track in tracks)
                {
                    var box = new Rect(
                        (int)track.BoundingBox.X,
                        (int)track.BoundingBox.Y,
                        (int)track.BoundingBox.Width,
                        (int)track.BoundingBox.Height);

                    Cv2.Rectangle(
                        frame, box, Scalar.LimeGreen, 2);

                    string label =
                        $"ID {track.Id} | {track.ClassName}";

                    if (track.MissedFrames == 0 &&
                        vehicleMotions.TryGetValue(
                            track.Id, out var motion))
                    {
                        label += $" | ~{motion.SpeedKmh:F1} km/h";
                    }
                    else
                    {
                        label += " | Speed: --";
                    }

                    Cv2.PutText(
                        frame,
                        label,
                        new Point(
                            Math.Max(0, box.X),
                            Math.Max(25, box.Y - 10)),
                        HersheyFonts.HersheySimplex,
                        0.6,
                        Scalar.Red,
                        2);

                    // Draw bottom-centre anchor point.
                    Cv2.Circle(
                        frame,
                        new Point(
                            (int)track.AnchorPoint.X,
                            (int)track.AnchorPoint.Y),
                        5,
                        Scalar.Blue,
                        -1);
                }

                // 11. Write the processed frame.
                writer.Write(frame);

                if (frameIndex % 10 == 0)
                {
                    Console.WriteLine(
                        $"Processed frame: {frameIndex}");
                }
            }

            // 12. Report completion.
            Console.WriteLine();
            Console.WriteLine("=================================");
            Console.WriteLine("Video tracking completed!");
            Console.WriteLine($"Frames processed: {frameIndex}");
            Console.WriteLine($"Output saved to: {outputPath}");
            Console.WriteLine("=================================");
        }
    }

    // Motion information for one tracked vehicle.
    public class VehicleMotion
    {
        public int TrackId { get; private set; }

        public Point2f PreviousWorldPosition { get; private set; }

        public int PreviousFrame { get; private set; }

        public float SpeedKmh { get; private set; }

        public VehicleMotion(
            int trackId,
            Point2f initialPosition,
            int initialFrame)
        {
            TrackId = trackId;
            PreviousWorldPosition = initialPosition;
            PreviousFrame = initialFrame;
            SpeedKmh = 0f;
        }

        public void Update(
            Point2f currentPosition,
            int currentFrame,
            float speedKmh)
        {
            PreviousWorldPosition = currentPosition;
            PreviousFrame = currentFrame;
            SpeedKmh = speedKmh;
        }

        public void Reset(
            Point2f currentPosition,
            int currentFrame)
        {
            PreviousWorldPosition = currentPosition;
            PreviousFrame = currentFrame;
            SpeedKmh = 0f;
        }
    }
}
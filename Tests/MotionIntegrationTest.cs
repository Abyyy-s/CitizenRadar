
using System;
using System.Collections.Generic;
using OpenCvSharp;
using CitizenRadar.Core;
using CitizenRadar.Models;

namespace CitizenRadar.Tests;

public static class MotionIntegrationTest
{
    public static void Run()
    {
        Console.WriteLine("=================================");
        Console.WriteLine(" CitizenRadar Motion Integration");
        Console.WriteLine("=================================");

        // Step 1: Set up calibration points.
        Point2f[] imagePoints =
        {
            new Point2f(100, 250),
            new Point2f(1800, 250),
            new Point2f(1800, 550),
            new Point2f(100, 550)
        };

        Point2f[] worldPoints =
        {
            new Point2f(0, 0),
            new Point2f(10, 0),
            new Point2f(10, 20),
            new Point2f(0, 20)
        };

        var calibrationData = new CalibrationData
        {
            ImagePoints = imagePoints,
            WorldPoints = worldPoints
        };

        var calibrator = new Calibrator(calibrationData);

        // Step 2: Create the speed estimator.
        var speedEstimator = new SpeedEstimator(30f);

        // Step 3: Store motion information for each vehicle ID.
        var motions = new Dictionary<int, VehicleMotion>();

        int vehicleId = 1;

        // Simulate the same vehicle at two different frames.
        Point2f firstPixel = new Point2f(500, 300);
        Point2f secondPixel = new Point2f(500, 330);

        int firstFrame = 0;
        int secondFrame = 30;

        // Convert pixel positions into world coordinates.
        Point2f firstWorld = calibrator.PixelToWorld(firstPixel);
        Point2f secondWorld = calibrator.PixelToWorld(secondPixel);

        // Store the first position.
        motions[vehicleId] = new VehicleMotion(
            vehicleId,
            firstWorld,
            firstFrame
        );

        // Calculate speed using the second position.
        VehicleMotion motion = motions[vehicleId];

        int frameDifference = secondFrame - motion.PreviousFrame;

        float speed = speedEstimator.CalculateSpeed(
            motion.PreviousWorldPosition,
            secondWorld,
            frameDifference
        );

        motion.Update(secondWorld, secondFrame, speed);

        Console.WriteLine($"Vehicle ID: {motion.TrackId}");
        Console.WriteLine($"First world position: ({firstWorld.X:F2}, {firstWorld.Y:F2}) m");
        Console.WriteLine($"Second world position: ({secondWorld.X:F2}, {secondWorld.Y:F2}) m");
        Console.WriteLine($"Frames elapsed: {frameDifference}");
        Console.WriteLine($"Estimated speed: {motion.SpeedKmh:F2} km/h");

        Console.WriteLine();
        Console.WriteLine("Motion integration test completed.");
        Console.WriteLine("=================================");
    }
}
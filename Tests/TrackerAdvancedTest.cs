using System;
using System.Collections.Generic;
using OpenCvSharp;
using CitizenRadar.Models;

namespace CitizenRadar.Tests;

public static class TrackerAdvancedTest
{
    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("     CITIZENRADAR ADVANCED TEST");
        Console.WriteLine("======================================");

        var tracker = new CitizenRadar.Core.Tracker(
            iouThreshold: 0.3f,
            maxMissedFrames: 2,
            maxAnchorDistance: 100f
        );


        // ==============================
        // FRAME 1
        // Two cars appear
        // ==============================

        Console.WriteLine();
        Console.WriteLine("FRAME 1");

        var frame1 = new List<Detection>
        {
            CreateDetection(100, 100),
            CreateDetection(300, 100)
        };

        tracker.Update(frame1, 1);

        PrintTracks(tracker);


        // ==============================
        // FRAME 2
        // Both cars move closer
        // ==============================

        Console.WriteLine();
        Console.WriteLine("FRAME 2");

        var frame2 = new List<Detection>
        {
            CreateDetection(120, 105),
            CreateDetection(280, 105)
        };

        tracker.Update(frame2, 2);

        PrintTracks(tracker);


        // ==============================
        // FRAME 3
        // Cars are now closer
        // ==============================

        Console.WriteLine();
        Console.WriteLine("FRAME 3");

        var frame3 = new List<Detection>
        {
            CreateDetection(145, 110),
            CreateDetection(255, 110)
        };

        tracker.Update(frame3, 3);

        PrintTracks(tracker);


        // ==============================
        // FRAME 4
        // Cars cross / become confusing
        // ==============================

        Console.WriteLine();
        Console.WriteLine("FRAME 4");

        var frame4 = new List<Detection>
        {
            CreateDetection(175, 115),
            CreateDetection(225, 115)
        };

        tracker.Update(frame4, 4);

        PrintTracks(tracker);


        // ==============================
        // FRAME 5
        // Cars continue
        // ==============================

        Console.WriteLine();
        Console.WriteLine("FRAME 5");

        var frame5 = new List<Detection>
        {
            CreateDetection(205, 120),
            CreateDetection(195, 120)
        };

        tracker.Update(frame5, 5);

        PrintTracks(tracker);


        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("       ADVANCED TEST COMPLETE");
        Console.WriteLine("======================================");
    }


  private static Detection CreateDetection(
    float x,
    float y)
{
    return new Detection(
        2,
        "car",
        0.95f,
        new Rect2f(
            x,
            y,
            80,
            50
        )
    );
}


    private static void PrintTracks(
    CitizenRadar.Core.Tracker tracker)
{
    foreach (var track in tracker.Tracks)
    {
        Console.WriteLine(
            $"Track ID: {track.Id} | " +
            $"Class: {track.ClassName} | " +
            $"State: {track.State} | " +
            $"Position: ({track.AnchorPoint.X:F1}, {track.AnchorPoint.Y:F1})"
        );

        Console.WriteLine("  History:");

        foreach (var point in track.History)
        {
            Console.WriteLine(
                $"    ({point.X:F1}, {point.Y:F1})"
            );
        }
    }
}
}
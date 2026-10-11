using CitizenRadar.Core;
using CitizenRadar.Models;
using OpenCvSharp;

namespace CitizenRadar.Tests;

public static class TrackerTest
{
    public static void Run()
    {
        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("      CITIZENRADAR TRACKER TEST");
        Console.WriteLine("======================================");

       var tracker = new CitizenRadar.Core.Tracker(
    iouThreshold: 0.3f,
    maxMissedFrames: 2
);

        // -----------------------------------------
        // FRAME 1
        // -----------------------------------------

        Console.WriteLine();
        Console.WriteLine("FRAME 1");

        var frame1 = new List<Detection>
        {
            CreateCar(100, 100)
        };

        tracker.Update(frame1, 1);

        PrintTracks(tracker);


        // -----------------------------------------
        // FRAME 2
        // -----------------------------------------

        Console.WriteLine();
        Console.WriteLine("FRAME 2");

        var frame2 = new List<Detection>
        {
            CreateCar(105, 102)
        };

        tracker.Update(frame2, 2);

        PrintTracks(tracker);


        // -----------------------------------------
        // FRAME 3
        // -----------------------------------------

        Console.WriteLine();
        Console.WriteLine("FRAME 3");

        var frame3 = new List<Detection>
        {
            CreateCar(110, 104),

            CreateCar(
                400,
                200
            )
        };

        tracker.Update(frame3, 3);

        PrintTracks(tracker);


        // -----------------------------------------
        // FRAME 4
        // Car A disappears
        // -----------------------------------------

        Console.WriteLine();
        Console.WriteLine("FRAME 4");

        var frame4 = new List<Detection>
        {
            CreateCar(405, 202)
        };

        tracker.Update(frame4, 4);

        PrintTracks(tracker);


        // -----------------------------------------
        // FRAME 5
        // Car A returns
        // -----------------------------------------

        Console.WriteLine();
        Console.WriteLine("FRAME 5");

        var frame5 = new List<Detection>
        {
            CreateCar(120, 108),

            CreateCar(410, 204)
        };

        tracker.Update(frame5, 5);

        PrintTracks(tracker);


        Console.WriteLine();
        Console.WriteLine("======================================");
        Console.WriteLine("             TEST COMPLETE");
        Console.WriteLine("======================================");
    }

    private static Detection CreateCar(
        float x,
        float y)
    {
        return new Detection(
            classId: 2,
            className: "car",
            confidence: 0.95f,
            boundingBox: new Rect2f(
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
                $"Missed: {track.MissedFrames} | " +
                $"Position: ({track.AnchorPoint.X:0.0}, {track.AnchorPoint.Y:0.0})"
            );
        }
    }
}
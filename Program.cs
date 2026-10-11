
using CitizenRadar.Models;
using OpenCvSharp;

var tracker = new CitizenRadar.Core.Tracker();

var frame1 = new List<Detection>
{
    new Detection(2, "car", 0.95f, new Rect2f(100, 100, 80, 60)),
    new Detection(2, "car", 0.93f, new Rect2f(400, 100, 80, 60))
};

tracker.Update(frame1, 1);

Console.WriteLine("Frame 1:");
foreach (var track in tracker.Tracks)
{
    Console.WriteLine($"ID: {track.Id}, X: {track.AnchorPoint.X}");
}

var frame2 = new List<Detection>
{
    new Detection(2, "car", 0.95f, new Rect2f(150, 100, 80, 60)),
    new Detection(2, "car", 0.93f, new Rect2f(350, 100, 80, 60))
};

tracker.Update(frame2, 2);

Console.WriteLine("\nFrame 2:");
foreach (var track in tracker.Tracks)
{
    Console.WriteLine($"ID: {track.Id}, X: {track.AnchorPoint.X}");
}

Console.WriteLine("\nTest completed.");


using System.Collections.Generic;
using System.Linq;
using CitizenRadar.Models;
using OpenCvSharp;
using Xunit;

namespace CitizenRadar.Tracking.Tests;

public class TrackerTests
{
    // Test 1: A vehicle keeps the same ID across frames.
    [Fact]
    public void Vehicle_ShouldKeepSameId_AcrossFrames()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        var frame1 = new List<Detection>
        {
            new Detection(2, "car", 0.92f,
                new Rect2f(100, 100, 80, 60))
        };

        var tracks1 = tracker.Update(frame1, 1);
        int originalId = tracks1[0].Id;

        var frame2 = new List<Detection>
        {
            new Detection(2, "car", 0.90f,
                new Rect2f(105, 102, 80, 60))
        };

        var tracks2 = tracker.Update(frame2, 2);

        Assert.Equal(originalId, tracks2[0].Id);
        Assert.Equal(2, tracks2[0].HitCount);
        Assert.Equal(TrackState.Active, tracks2[0].State);
    }

    // Test 2: Two vehicles receive different IDs.
    [Fact]
    public void TwoVehicles_ShouldHaveDifferentIds()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        var detections = new List<Detection>
        {
            new Detection(2, "car", 0.95f,
                new Rect2f(100, 100, 80, 60)),
            new Detection(2, "car", 0.93f,
                new Rect2f(400, 100, 80, 60))
        };

        var tracks = tracker.Update(detections, 1);

        Assert.Equal(2, tracks.Count);
        Assert.NotEqual(tracks[0].Id, tracks[1].Id);
    }

    // Test 3: History does not exceed its maximum length.
    [Fact]
    public void History_ShouldNotExceedMaximumLength()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        for (int frame = 1; frame <= 35; frame++)
        {
            var detections = new List<Detection>
            {
                new Detection(2, "car", 0.92f,
                    new Rect2f(100, 100, 80, 60))
            };

            tracker.Update(detections, frame);
        }

        var track = tracker.Tracks[0];

        Assert.Equal(30, track.History.Count);
        Assert.True(track.History.Count <= track.MaxHistoryLength);
    }

    // Test 4: A missing vehicle enters the Coasting state.
    [Fact]
    public void MissingDetection_ShouldSetTrackToCoasting()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        var detections = new List<Detection>
        {
            new Detection(2, "car", 0.92f,
                new Rect2f(100, 100, 80, 60))
        };

        tracker.Update(detections, 1);
        tracker.Update(new List<Detection>(), 2);

        var track = tracker.Tracks[0];

        Assert.Equal(TrackState.Coasting, track.State);
        Assert.Equal(1, track.MissedFrames);
    }

    // Test 5: A returning vehicle recovers its original ID.
    [Fact]
    public void VehicleReturningAfterMiss_ShouldRecoverOriginalId()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        var firstDetection = new List<Detection>
        {
            new Detection(2, "car", 0.92f,
                new Rect2f(100, 100, 80, 60))
        };

        var firstTracks = tracker.Update(firstDetection, 1);
        int originalId = firstTracks[0].Id;

        tracker.Update(new List<Detection>(), 2);

        var returningDetection = new List<Detection>
        {
            new Detection(2, "car", 0.91f,
                new Rect2f(110, 102, 80, 60))
        };

        tracker.Update(returningDetection, 3);

        var recoveredTrack = tracker.Tracks[0];

        Assert.Equal(originalId, recoveredTrack.Id);
        Assert.Equal(TrackState.Active, recoveredTrack.State);
        Assert.Equal(0, recoveredTrack.MissedFrames);
    }

    // Test 6: Both track IDs remain present during overlapping detections.
    [Fact]
    public void TwoVehicles_CrossingPaths_ShouldKeepBothTrackIds()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        var frame1 = new List<Detection>
        {
            new Detection(2, "car", 0.95f,
                new Rect2f(100, 100, 80, 60)),
            new Detection(2, "car", 0.93f,
                new Rect2f(300, 100, 80, 60))
        };

        var tracks1 = tracker.Update(frame1, 1);
        int carAId = tracks1[0].Id;
        int carBId = tracks1[1].Id;

        var frame2 = new List<Detection>
        {
            new Detection(2, "car", 0.95f,
                new Rect2f(150, 100, 80, 60)),
            new Detection(2, "car", 0.93f,
                new Rect2f(250, 100, 80, 60))
        };

        tracker.Update(frame2, 2);

        var frame3 = new List<Detection>
        {
            new Detection(2, "car", 0.93f,
                new Rect2f(200, 100, 80, 60)),
            new Detection(2, "car", 0.95f,
                new Rect2f(200, 100, 80, 60))
        };

        tracker.Update(frame3, 3);

        Assert.Contains(tracker.Tracks, t => t.Id == carAId);
        Assert.Contains(tracker.Tracks, t => t.Id == carBId);
        Assert.Equal(2, tracker.Tracks.Count);
    }

    // Test 7: Vehicles cross and separate; verify their final positions.
    [Fact]
    public void TwoVehicles_CrossAndSeparate_ShouldKeepTheirIds()
    {
        var tracker = new CitizenRadar.Core.Tracker();

        // Frame 1: Car A on the left, Car B on the right.
        var frame1 = new List<Detection>
        {
            new Detection(2, "car", 0.95f,
                new Rect2f(100, 100, 80, 60)),
            new Detection(2, "car", 0.93f,
                new Rect2f(300, 100, 80, 60))
        };

        var tracks1 = tracker.Update(frame1, 1);
        int carAId = tracks1[0].Id;
        int carBId = tracks1[1].Id;

        // Frame 2: Cars move toward each other.
        var frame2 = new List<Detection>
        {
            new Detection(2, "car", 0.95f,
                new Rect2f(150, 100, 80, 60)),
            new Detection(2, "car", 0.93f,
                new Rect2f(250, 100, 80, 60))
        };

        tracker.Update(frame2, 2);

        // Frame 3: Their detections overlap completely.
        var frame3 = new List<Detection>
        {
            new Detection(2, "car", 0.93f,
                new Rect2f(200, 100, 80, 60)),
            new Detection(2, "car", 0.95f,
                new Rect2f(200, 100, 80, 60))
        };

        tracker.Update(frame3, 3);

        // Frame 4: The cars move apart.
        var frame4 = new List<Detection>
        {
            new Detection(2, "car", 0.93f,
                new Rect2f(150, 100, 80, 60)),
            new Detection(2, "car", 0.95f,
                new Rect2f(250, 100, 80, 60))
        };

        tracker.Update(frame4, 4);

        var carA = tracker.Tracks.Single(t => t.Id == carAId);
        var carB = tracker.Tracks.Single(t => t.Id == carBId);

        // Car A should finish on the right; Car B on the left.
        Assert.True(carA.AnchorPoint.X > carB.AnchorPoint.X);
        Assert.Equal(290f, carA.AnchorPoint.X);
        Assert.Equal(190f, carB.AnchorPoint.X);
    }
}

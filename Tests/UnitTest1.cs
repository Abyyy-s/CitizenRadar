
using System.Collections.Generic;
using System.Linq;
using Xunit;
using OpenCvSharp;
using CitizenRadar.Models;
using CitizenRadar.Core;
using Tracker = CitizenRadar.Core.Tracker;

public class UnitTest1
{
    private static Detection CreateCar(float x, float y = 100)
    {
        return new Detection(
            2,
            "car",
            0.95f,
            new Rect2f(x, y, 80, 60)
        );
    }

    private static Detection CreateTruck(float x, float y = 100)
    {
        return new Detection(
            7,
            "truck",
            0.90f,
            new Rect2f(x, y, 100, 70)
        );
    }

    [Fact]
    public void Vehicle_ShouldCreateTrackWithUniqueId()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection> { CreateCar(100) },
            1
        );

        Assert.Single(tracker.Tracks);
        Assert.True(tracker.Tracks[0].Id > 0);
        Assert.Equal("car", tracker.Tracks[0].ClassName);
    }

    [Fact]
    public void TwoVehicles_ShouldHaveDifferentIds()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection>
            {
                CreateCar(100),
                CreateCar(400)
            },
            1
        );

        Assert.Equal(2, tracker.Tracks.Count);
        Assert.NotEqual(
            tracker.Tracks[0].Id,
            tracker.Tracks[1].Id
        );
    }

    [Fact]
    public void Vehicle_ShouldKeepSameIdAcrossFrames()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection> { CreateCar(100) },
            1
        );

        int originalId = tracker.Tracks[0].Id;

        tracker.Update(
            new List<Detection> { CreateCar(110) },
            2
        );

        Assert.Single(tracker.Tracks);
        Assert.Equal(originalId, tracker.Tracks[0].Id);
        Assert.Equal(2, tracker.Tracks[0].HitCount);
    }

    [Fact]
    public void Track_ShouldStoreAnchorPointAtBottomCenter()
    {
        var detection = CreateCar(100, 50);
        var track = new Track(1, detection, 1);

        Assert.Equal(140f, track.AnchorPoint.X);
        Assert.Equal(110f, track.AnchorPoint.Y);
    }

    [Fact]
    public void TrackHistory_ShouldRecordVehiclePositions()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection> { CreateCar(100) },
            1
        );

        tracker.Update(
            new List<Detection> { CreateCar(120) },
            2
        );

        Assert.Equal(2, tracker.Tracks[0].History.Count);
    }

    [Fact]
    public void TrackHistory_ShouldRespectMaximumLength()
    {
        var track = new Track(1, CreateCar(100), 1);
        track.MaxHistoryLength = 3;

        track.Update(CreateCar(110), 2);
        track.Update(CreateCar(120), 3);
        track.Update(CreateCar(130), 4);
        track.Update(CreateCar(140), 5);

        Assert.Equal(3, track.History.Count);
        Assert.Equal(180f, track.History.Last().X);
    }

    [Fact]
    public void MissingDetection_ShouldMoveTrackToCoasting()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection> { CreateCar(100) },
            1
        );

        tracker.Update(
            new List<Detection>(),
            2
        );

        Assert.Single(tracker.Tracks);
        Assert.Equal(
            TrackState.Coasting,
            tracker.Tracks[0].State
        );
        Assert.Equal(1, tracker.Tracks[0].MissedFrames);
    }

    [Fact]
    public void VehicleReturningAfterMiss_ShouldReuseOriginalId()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection> { CreateCar(100) },
            1
        );

        int originalId = tracker.Tracks[0].Id;

        tracker.Update(new List<Detection>(), 2);

        tracker.Update(
            new List<Detection> { CreateCar(105) },
            3
        );

        Assert.Contains(
            tracker.Tracks,
            track => track.Id == originalId
        );
    }

    [Fact]
    public void ConsecutiveEmptyFrames_ShouldNotCreateTracks()
    {
        var tracker = new Tracker();

        tracker.Update(new List<Detection>(), 1);
        tracker.Update(new List<Detection>(), 2);
        tracker.Update(new List<Detection>(), 3);

        Assert.Empty(tracker.Tracks);
    }

    [Fact]
    public void DifferentVehicleClasses_ShouldHaveSeparateTracks()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection>
            {
                CreateCar(100),
                CreateTruck(400)
            },
            1
        );

        Assert.Equal(2, tracker.Tracks.Count);
        Assert.Contains(
            tracker.Tracks,
            track => track.ClassName == "car"
        );
        Assert.Contains(
            tracker.Tracks,
            track => track.ClassName == "truck"
        );
    }

    [Fact]
    public void TwoVehiclesCrossAndSeparate_ShouldKeepTwoOriginalTracks()
    {
        var tracker = new Tracker();

        tracker.Update(
            new List<Detection>
            {
                CreateCar(100),
                CreateCar(400)
            },
            1
        );

        Assert.Equal(2, tracker.Tracks.Count);

        int firstId = tracker.Tracks[0].Id;
        int secondId = tracker.Tracks[1].Id;

        tracker.Update(
            new List<Detection>
            {
                CreateCar(150),
                CreateCar(350)
            },
            2
        );

        tracker.Update(
            new List<Detection>
            {
                CreateCar(200),
                CreateCar(300)
            },
            3
        );

        tracker.Update(
            new List<Detection>
            {
                CreateCar(250),
                CreateCar(250)
            },
            4
        );

        tracker.Update(
            new List<Detection>
            {
                CreateCar(300),
                CreateCar(200)
            },
            5
        );

        Assert.Equal(2, tracker.Tracks.Count);

        Assert.Contains(
            tracker.Tracks,
            track => track.Id == firstId
        );

        Assert.Contains(
            tracker.Tracks,
            track => track.Id == secondId
        );
    }

    [Fact]
    public void Track_ShouldBecomeLostAfterMissLimitExceeded()
    {
        var track = new Track(1, CreateCar(100), 1);

        track.MarkMissed(2);
        Assert.Equal(TrackState.Coasting, track.State);

        track.MarkMissed(2);
        Assert.Equal(TrackState.Coasting, track.State);

        track.MarkMissed(2);
        Assert.Equal(TrackState.Lost, track.State);
    }
}

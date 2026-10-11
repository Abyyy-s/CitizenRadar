using OpenCvSharp;

namespace CitizenRadar.Models;

public class VehicleMotion { public int TrackId { get; }

public Point2f PreviousWorldPosition { get; private set; }

public int PreviousFrame { get; private set; }

public float SpeedKmh { get; private set; }

public VehicleMotion(int trackId, Point2f worldPosition, int frameIndex) { TrackId = trackId; PreviousWorldPosition = worldPosition; PreviousFrame = frameIndex; SpeedKmh = 0f; }

public void Update(Point2f worldPosition, int frameIndex, float speedKmh) { PreviousWorldPosition = worldPosition; PreviousFrame = frameIndex; SpeedKmh = speedKmh; } }
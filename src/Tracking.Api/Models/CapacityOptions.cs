namespace Tracking.Api.Models;

public sealed class CapacityOptions
{
    public int TrackingEventQueue { get; init; } = 500000;
}
using Tracking.Api.Requests;
using Tracking.Api.Services;

namespace Tracking.Api.Tests.TestDoubles;

public sealed class FakeTrackingEventQueue : ITrackingEventQueue
{
    public List<TrackingEventCommand> Commands { get; } = new();
    public bool ShouldReject { get; set; }

    public ValueTask<bool> EnqueueAsync(TrackingEventCommand command, CancellationToken cancellationToken)
    {
        if (ShouldReject)
        {
            return ValueTask.FromResult(false);
        }

        Commands.Add(command);
        return ValueTask.FromResult(true);
    }
}

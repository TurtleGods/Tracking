using System.ComponentModel.DataAnnotations;
using Tracking.Api.Models;

namespace Tracking.Api.Requests;

public sealed class CreateTrackingSessionRequest
{
    
    [Required]
    public required string Production{get;set;}

    public DateTime? StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? EndedAt { get; set; }

    public TrackingSession ToTrackingSession(Guid entityId)
    {
        var started = StartedAt ?? DateTime.UtcNow;
        var lastActivity = LastActivityAt ?? started;
        return new TrackingSession
        {
            SessionId = Guid.NewGuid(),
            EntityId = entityId,
            StartedAt = started,
            LastActivityAt = lastActivity,
            EndedAt = EndedAt,
            CreatedAt = DateTime.UtcNow
        };
    }
}

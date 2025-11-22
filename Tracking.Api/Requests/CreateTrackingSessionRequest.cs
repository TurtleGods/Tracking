using System.ComponentModel.DataAnnotations;
using Tracking.Api.Models;

namespace Tracking.Api.Requests;

public sealed class CreateTrackingSessionRequest
{
    [Required]
    public long UserId { get; set; }

    [Required]
    public long CompanyId { get; set; }

    public DateTime? StartedAt { get; set; }
    public DateTime? LastActivityAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int TotalEvents { get; set; }
    public int TotalViews { get; set; }
    public int TotalClicks { get; set; }
    public int TotalExposes { get; set; }
    public int TotalDisappears { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string DeviceModel { get; set; } = string.Empty;
    public string EntryPage { get; set; } = string.Empty;
    public string ExitPage { get; set; } = string.Empty;

    public TrackingSession ToTrackingSession(Guid entityId)
    {
        var started = StartedAt ?? DateTime.UtcNow;
        var lastActivity = LastActivityAt ?? started;
        return new TrackingSession
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            UserId = UserId,
            CompanyId = CompanyId,
            StartedAt = started,
            LastActivityAt = lastActivity,
            EndedAt = EndedAt ?? lastActivity,
            TotalEvents = TotalEvents,
            TotalViews = TotalViews,
            TotalClicks = TotalClicks,
            TotalExposes = TotalExposes,
            TotalDisappears = TotalDisappears,
            DeviceType = DeviceType,
            DeviceModel = DeviceModel,
            EntryPage = EntryPage,
            ExitPage = ExitPage,
            CreatedAt = DateTime.UtcNow
        };
    }
}

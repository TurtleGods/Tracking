using System.ComponentModel.DataAnnotations;
using Tracking.Api.Models;

namespace Tracking.Api.Requests;

public sealed class CreateTrackingEventRequest
{
    [Required]
    public Guid SessionId { get; set; }

    [Required]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string EventName { get; set; } = string.Empty;

    public string PageName { get; set; } = string.Empty;
    public string ComponentName { get; set; } = string.Empty;
    public DateTime? Timestamp { get; set; }
    public string Refer { get; set; } = string.Empty;
    public int ExposeTime { get; set; }
    public long UserId { get; set; }
    public long CompanyId { get; set; }
    public string DeviceType { get; set; } = string.Empty;
    public string OsVersion { get; set; } = string.Empty;
    public string BrowserVersion { get; set; } = string.Empty;
    public string NetworkType { get; set; } = string.Empty;
    public string NetworkEffectiveType { get; set; } = string.Empty;
    public string PageUrl { get; set; } = string.Empty;
    public string PageTitle { get; set; } = string.Empty;
    public int ViewportHeight { get; set; }
    public string Properties { get; set; } = "{}";

    public TrackingEvent ToTrackingEvent(Guid entityId)
    {
        return new TrackingEvent
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            SessionId = SessionId,
            EventType = EventType,
            EventName = EventName,
            PageName = PageName,
            ComponentName = ComponentName,
            Timestamp = Timestamp ?? DateTime.UtcNow,
            Refer = Refer,
            ExposeTime = ExposeTime,
            UserId = UserId,
            CompanyId = CompanyId,
            DeviceType = DeviceType,
            OsVersion = OsVersion,
            BrowserVersion = BrowserVersion,
            NetworkType = NetworkType,
            NetworkEffectiveType = NetworkEffectiveType,
            PageUrl = PageUrl,
            PageTitle = PageTitle,
            ViewportHeight = ViewportHeight,
            Properties = Properties
        };
    }
}

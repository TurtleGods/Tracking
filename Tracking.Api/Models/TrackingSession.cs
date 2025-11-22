using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record TrackingSession
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("company_id")]
    public long CompanyId { get; init; }

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; init; }

    [JsonPropertyName("last_activity_at")]
    public DateTime LastActivityAt { get; init; }

    [JsonPropertyName("ended_at")]
    public DateTime EndedAt { get; init; }

    [JsonPropertyName("total_events")]
    public int TotalEvents { get; init; }

    [JsonPropertyName("total_views")]
    public int TotalViews { get; init; }

    [JsonPropertyName("total_clicks")]
    public int TotalClicks { get; init; }

    [JsonPropertyName("total_exposes")]
    public int TotalExposes { get; init; }

    [JsonPropertyName("total_disappears")]
    public int TotalDisappears { get; init; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; init; } = string.Empty;

    [JsonPropertyName("device_model")]
    public string DeviceModel { get; init; } = string.Empty;

    [JsonPropertyName("entry_page")]
    public string EntryPage { get; init; } = string.Empty;

    [JsonPropertyName("exit_page")]
    public string ExitPage { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

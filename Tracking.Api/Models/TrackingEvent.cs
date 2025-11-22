using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record TrackingEvent
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("session_id")]
    public Guid SessionId { get; init; }

    [JsonPropertyName("event_type")]
    public string EventType { get; init; } = string.Empty;

    [JsonPropertyName("event_name")]
    public string EventName { get; init; } = string.Empty;

    [JsonPropertyName("page_name")]
    public string PageName { get; init; } = string.Empty;

    [JsonPropertyName("component_name")]
    public string ComponentName { get; init; } = string.Empty;

    [JsonPropertyName("timestamp")]
    public DateTime Timestamp { get; init; }

    [JsonPropertyName("refer")]
    public string Refer { get; init; } = string.Empty;

    [JsonPropertyName("expose_time")]
    public int ExposeTime { get; init; }

    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("company_id")]
    public long CompanyId { get; init; }

    [JsonPropertyName("device_type")]
    public string DeviceType { get; init; } = string.Empty;

    [JsonPropertyName("os_version")]
    public string OsVersion { get; init; } = string.Empty;

    [JsonPropertyName("browser_version")]
    public string BrowserVersion { get; init; } = string.Empty;

    [JsonPropertyName("network_type")]
    public string NetworkType { get; init; } = string.Empty;

    [JsonPropertyName("network_effective_type")]
    public string NetworkEffectiveType { get; init; } = string.Empty;

    [JsonPropertyName("page_url")]
    public string PageUrl { get; init; } = string.Empty;

    [JsonPropertyName("page_title")]
    public string PageTitle { get; init; } = string.Empty;

    [JsonPropertyName("viewport_height")]
    public int ViewportHeight { get; init; }

    [JsonPropertyName("properties")]
    public string Properties { get; init; } = "{}";
}

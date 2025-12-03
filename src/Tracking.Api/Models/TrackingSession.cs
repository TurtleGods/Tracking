using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record TrackingSession
{
    [JsonPropertyName("session_id")]
    public Guid SessionId { get; init; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("user_id")]
    public Guid EmployeeId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("started_at")]
    public DateTime StartedAt { get; init; }

    [JsonPropertyName("last_activity_at")]
    public DateTime LastActivityAt { get; init; }

    [JsonPropertyName("ended_at")]
    public DateTime? EndedAt { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

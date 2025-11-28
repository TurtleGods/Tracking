using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record DashboardEditor
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("user_email")]
    public string UserEmail { get; init; } = string.Empty;

    [JsonPropertyName("added_at")]
    public DateTime AddedAt { get; init; }

    [JsonPropertyName("added_by")]
    public string AddedBy { get; init; } = string.Empty;
}

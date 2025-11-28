using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record MainEntity
{
    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }


    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("production")]
    public string Production { get; init; } = string.Empty;

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }
}

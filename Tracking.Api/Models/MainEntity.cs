using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record MainEntity
{
    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("creator_id")]
    public ulong CreatorId { get; init; }

    [JsonPropertyName("company_id")]
    public Guid CompanyId { get; init; }

    [JsonPropertyName("creator_email")]
    public string CreatorEmail { get; init; } = string.Empty;

    [JsonPropertyName("production")]
    public string Production { get; init; } = string.Empty;

    [JsonPropertyName("panels")]
    public string Panels { get; init; } = "{}";

    [JsonPropertyName("collaborators")]
    public string Collaborators { get; init; } = "[]";

    [JsonPropertyName("visibility")]
    public string Visibility { get; init; } = "private";

    [JsonPropertyName("is_shared")]
    public bool IsShared { get; init; }

    [JsonPropertyName("shared_token")]
    public Guid SharedToken { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }

    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; init; }
}

using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

[ExcludeFromCodeCoverage]
public sealed record DashboardFavorite
{
    [JsonPropertyName("id")]
    public Guid Id { get; init; }

    [JsonPropertyName("entity_id")]
    public Guid EntityId { get; init; }

    [JsonPropertyName("user_id")]
    public long UserId { get; init; }

    [JsonPropertyName("created_at")]
    public DateTime CreatedAt { get; init; }
}

using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record FeatureUsage
{
    [JsonPropertyName("event_name")]
    public string EventName { get; init; } = string.Empty;

    [JsonPropertyName("count")]
    public ulong Count { get; init; }

    [JsonPropertyName("percentage")]
    public double Percentage { get; init; }
}

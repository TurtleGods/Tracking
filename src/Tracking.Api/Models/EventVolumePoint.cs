using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record EventVolumePoint
{
    [JsonPropertyName("bucket_start_utc")]
    public DateTime BucketStartUtc { get; init; }

    [JsonPropertyName("events")]
    public ulong Events { get; init; }
}

using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public sealed record DailyOverviewMetrics
{
    [JsonPropertyName("date_utc")]
    public DateTime DateUtc { get; init; }

    [JsonPropertyName("dau")]
    public ulong DailyActiveUsers { get; init; }

    [JsonPropertyName("total_events")]
    public ulong TotalEvents { get; init; }

    [JsonPropertyName("sessions")]
    public ulong Sessions { get; init; }

    [JsonPropertyName("active_companies")]
    public ulong ActiveCompanies { get; init; }
}

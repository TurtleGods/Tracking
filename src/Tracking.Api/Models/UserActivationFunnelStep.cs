using System.Text.Json.Serialization;

namespace Tracking.Api.Models;

public static class UserActivationFunnelStages
{
    public const string SessionStart = "session_start";
    public const string FirstEvent = "first_event";
    public const string MeaningfulEvent = "meaningful_event";

    public static readonly string[] Ordered = new[]
    {
        SessionStart,
        FirstEvent,
        MeaningfulEvent
    };
}

public sealed record UserActivationFunnelCount
{
    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("sessions")]
    public ulong Sessions { get; init; }
}

public sealed record UserActivationFunnelStep
{
    [JsonPropertyName("stage")]
    public string Stage { get; init; } = string.Empty;

    [JsonPropertyName("sessions")]
    public ulong Sessions { get; init; }

    [JsonPropertyName("conversion_from_previous")]
    public double ConversionFromPrevious { get; init; }
}

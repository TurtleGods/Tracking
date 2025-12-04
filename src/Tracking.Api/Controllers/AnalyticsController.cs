using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("analytics")]
public sealed class AnalyticsController : ControllerBase
{
    private readonly ITrackingRepository _repository;

    public AnalyticsController(ITrackingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet("overview")]
    public async Task<ActionResult<DailyOverviewMetrics>> GetOverview([FromQuery] DateTime? date = null, CancellationToken cancellationToken = default)
    {
        var targetDate = date.HasValue
            ? DateTime.SpecifyKind(date.Value.Date, DateTimeKind.Utc)
            : DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);

        var overview = await _repository.GetDailyOverviewAsync(targetDate, cancellationToken);
        return Ok(overview);
    }

    [HttpGet("event-volume")]
    public async Task<ActionResult<IEnumerable<EventVolumePoint>>> GetEventVolume(
        [FromQuery] string range = "24h",
        [FromQuery] string? eventType = null,
        [FromQuery] string? production = null,
        [FromQuery] DateTime? endUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end, bucket) = NormalizeRange(range, endUtc);
        var points = await _repository.GetEventVolumeAsync(start, end, bucket, eventType, production, cancellationToken);
        return Ok(points);
    }

    [HttpGet("usage")]
    public async Task<ActionResult<IEnumerable<FeatureUsage>>> GetFeatureUsage(
        [FromQuery] string range = "7d",
        [FromQuery] string? eventType = null,
        [FromQuery] string? production = null,
        [FromQuery] DateTime? endUtc = null,
        CancellationToken cancellationToken = default)
    {
        var (start, end, _) = NormalizeRange(range, endUtc);
        var usage = await _repository.GetFeatureUsageAsync(start, end, eventType, production, cancellationToken);
        return Ok(usage);
    }

    private static (DateTime start, DateTime end, TimeSpan bucket) NormalizeRange(string range, DateTime? endUtc)
    {
        var end = DateTime.SpecifyKind(endUtc ?? DateTime.UtcNow, DateTimeKind.Utc);
        var normalized = string.IsNullOrWhiteSpace(range) ? "24h" : range.Trim().ToLowerInvariant();

        return normalized switch
        {
            "7d" or "7day" or "7days" or "7" => (end.AddDays(-7), end, TimeSpan.FromDays(1)),
            _ => (end.AddHours(-24), end, TimeSpan.FromHours(1))
        };
    }
}

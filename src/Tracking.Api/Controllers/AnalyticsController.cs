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
}

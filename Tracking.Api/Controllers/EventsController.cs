using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities/{entityId:guid}/events")]
public sealed class EventsController : ControllerBase
{
    private readonly ITrackingRepository _repository;

    public EventsController(ITrackingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackingEvent>>> Get(Guid entityId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var take = NormalizeLimit(limit);
        var eventsForEntity = await _repository.GetEventsAsync(entityId, take, cancellationToken);
        return Ok(eventsForEntity);
    }

    [HttpPost]
    public async Task<ActionResult<TrackingEvent>> Create(Guid entityId, [FromBody] CreateTrackingEventRequest request, CancellationToken cancellationToken = default)
    {
        var trackingEvent = request.ToTrackingEvent(entityId);
        await _repository.InsertEventAsync(trackingEvent, cancellationToken);
        return CreatedAtAction(nameof(Get), new { entityId, id = trackingEvent.Id }, trackingEvent);
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };
}

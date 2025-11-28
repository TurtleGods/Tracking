using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities/{sessionId:guid}/events")]
public sealed class EventsController : ControllerBase
{
    private readonly ITrackingRepository _repository;

    public EventsController(ITrackingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackingEvent>>> Get(Guid sessionId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var take = NormalizeLimit(limit);
        var eventsForEntity = await _repository.GetEventsBySessionAsync(sessionId, take, cancellationToken);
        return Ok(eventsForEntity);
    }

    [HttpPost]
    public async Task<ActionResult<TrackingEvent>> Create(Guid sessionId, [FromBody] CreateTrackingEventRequest request, CancellationToken cancellationToken = default)
    {
        var session = await _repository.GetEventBySessionIdAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return NotFound();
        }

        var trackingEvent = request.ToTrackingEvent(session.EntityId, sessionId);
        await _repository.InsertEventAsync(trackingEvent, cancellationToken);
        return CreatedAtAction(nameof(Get), new { sessionId, id = trackingEvent.Id }, trackingEvent);
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };
}

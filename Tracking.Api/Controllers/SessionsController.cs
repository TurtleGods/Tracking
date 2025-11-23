using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities/{entityId:guid}/sessions")]
public sealed class SessionsController : ControllerBase
{
    private readonly ITrackingRepository _repository;

    public SessionsController(ITrackingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TrackingSession>>> Get(Guid entityId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var take = NormalizeLimit(limit);
        var sessions = await _repository.GetSessionsAsync(entityId, take, cancellationToken);
        return Ok(sessions);
    }

    [HttpPost]
    public async Task<ActionResult<TrackingSession>> Create(Guid entityId, [FromBody] CreateTrackingSessionRequest request, CancellationToken cancellationToken = default)
    {
        var session = request.ToTrackingSession(entityId);
        await _repository.InsertSessionAsync(session, cancellationToken);
        return CreatedAtAction(nameof(Get), new { entityId, id = session.Id }, session);
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };
}

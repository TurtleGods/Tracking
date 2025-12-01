using Microsoft.AspNetCore.Mvc;
using System.Diagnostics.CodeAnalysis;
using System.IdentityModel.Tokens.Jwt;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;
using Tracking.Api.Services;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities")]
public sealed class EventsController : ControllerBase
{
    private readonly ITrackingRepository _repository;
    private readonly ITrackingEventQueue _eventQueue;

    public EventsController(ITrackingRepository repository, ITrackingEventQueue eventQueue)
    {
        _repository = repository;
        _eventQueue = eventQueue;
    }

    [HttpGet("{sessionId:guid}/events")]
    public async Task<ActionResult<IEnumerable<TrackingEvent>>> Get(Guid sessionId, [FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var take = NormalizeLimit(limit);
        var eventsForEntity = await _repository.GetEventsBySessionAsync(sessionId, take, cancellationToken);
        return Ok(eventsForEntity);
    }


    /// <summary>
    /// Creates a tracking event, optionally creating a new session if one does not exist.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("events")]
    [HttpPost("{sessionId:guid}/events")]
    public async Task<ActionResult> Create([FromRoute] Guid? sessionId, [FromBody] CreateTrackingEventRequest request, CancellationToken cancellationToken = default)
    {
        var companyIdClaim = ExtractCidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
        var employeeIdClaim= ExtractEidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
        if (string.IsNullOrWhiteSpace(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId)||string.IsNullOrEmpty(employeeIdClaim)||!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized("Missing or invalid session cookie. Please log in again.");
        }
        var enqueued = await _eventQueue.EnqueueAsync(new TrackingEventCommand(sessionId, companyId, employeeId, request), cancellationToken);
        if (!enqueued)
        {
            return StatusCode(StatusCodes.Status429TooManyRequests, "Event queue is full. Please retry later.");
        }

        return Accepted(new { status = "queued" });
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };

    [ExcludeFromCodeCoverage]
    private static string? ExtractCidFromCookie(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == "cid")?.Value;
        }
        catch
        {
            return null;
        }
    }
    [ExcludeFromCodeCoverage]
    private static string? ExtractEidFromCookie(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == "eid")?.Value;
        }
        catch
        {
            return null;
        }
    }
}

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities")]
public sealed class EventsController : ControllerBase
{
    private readonly ITrackingRepository _repository;
    private readonly ProductionOptions _productionOptions;
    private static readonly string[] DefaultProductionCodes = new[] { "PT", "PY", "FD" };

    public EventsController(ITrackingRepository repository, IOptions<ProductionOptions> productionOptions)
    {
        _repository = repository;
        _productionOptions = productionOptions.Value;
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
    public async Task<ActionResult<TrackingEvent>> Create([FromRoute] Guid? sessionId, [FromBody] CreateTrackingEventRequest request, CancellationToken cancellationToken = default)
    {
        var companyIdClaim = ExtractCidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
        var employeeIdClaim= ExtractEidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
        if (string.IsNullOrWhiteSpace(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId)||string.IsNullOrEmpty(employeeIdClaim)||!Guid.TryParse(employeeIdClaim, out var employeeId))
        {
            return Unauthorized("Missing or invalid session cookie. Please log in again.");
        }
        var requestedSessionId = sessionId ?? Guid.Empty;
        var existingSession = requestedSessionId != Guid.Empty
            ? await _repository.GetEventBySessionIdAsync(requestedSessionId, cancellationToken)
            : null;
        TrackingSession session;
        if (existingSession is null)
        {
            var entity = await GetOrCreateEntityAsync(companyId, request.Production, cancellationToken);
            var startedAt = request.Timestamp ?? DateTime.UtcNow;
            var newSessionId = requestedSessionId == Guid.Empty ? Guid.NewGuid() : requestedSessionId;

            session = new TrackingSession
            {
                SessionId = newSessionId,
                EntityId = entity.EntityId,
                EmployeeId = employeeId,
                CompanyId = companyId,
                StartedAt = startedAt,
                LastActivityAt = startedAt,
                EndedAt = null,
                CreatedAt = DateTime.UtcNow
            };

            await _repository.InsertSessionAsync(session, cancellationToken);
        }
        else
        {
            session = existingSession;
        }

        var trackingEvent = request.ToTrackingEvent(session.EntityId, session.SessionId, session.EmployeeId, session.CompanyId);
        await _repository.InsertEventAsync(trackingEvent, cancellationToken);
        return CreatedAtAction(nameof(Get), new { sessionId = session.SessionId, id = trackingEvent.Id }, trackingEvent);
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };

    /// <summary>
    /// Gets or creates the main entity for the given company and production.
    /// If not found, creates entities for all productions.
    /// </summary>
    /// <param name="companyId"></param>
    /// <param name="production"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task<MainEntity> GetOrCreateEntityAsync(Guid companyId, string production, CancellationToken cancellationToken)
    {
        var existing = await _repository.GetMainEntityByCompanyAndProductionAsync(companyId, production, cancellationToken);
        if (existing is not null)
        {
            return existing;
        }
        var productions = _productionOptions.Codes is { Length: > 0 } ? _productionOptions.Codes : DefaultProductionCodes;
        foreach (var prod in productions)
        {
            var _entity = new MainEntity
            {
                EntityId = CreateDeterministicEntityId(prod, companyId),
                CompanyId = companyId,
                Production = prod,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            await _repository.InsertMainEntityAsync(_entity, cancellationToken);
        }
        var entity = await _repository.GetMainEntityByCompanyAndProductionAsync(companyId, production, cancellationToken);

        return entity!;
    }

    private static Guid CreateDeterministicEntityId(string production, Guid companyId)
    {
        var key = $"{production}:{companyId}".ToLowerInvariant();
        using var md5 = MD5.Create();
        var bytes = md5.ComputeHash(Encoding.UTF8.GetBytes(key));
        return new Guid(bytes);
    }
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

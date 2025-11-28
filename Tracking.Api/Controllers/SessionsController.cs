using Microsoft.AspNetCore.Mvc;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Cryptography;
using System.Text;
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

    /// <summary>
    /// 1. If sessionId is provided, create session under that entity.
    /// 2. If sessionId is not provided, extract companyId from cookie, ensure
    ///   entities for that company, choose primary entity (or first one), create session under that entity.
    /// 3. If cookie is missing or invalid, return Unauthorized.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="request"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    [HttpPost("/sessions/{sessionId:guid}")]
    public async Task<ActionResult<TrackingSession>> Create([FromRoute] Guid? sessionId, [FromBody] CreateTrackingSessionRequest request, CancellationToken cancellationToken = default)
    {
        var targetEntity = sessionId.HasValue?sessionId:null;
        var companyId = Guid.Empty;
        if (targetEntity is null)
        {
            var companyIdClaim = ExtractCidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
            var employeeIdClaim= ExtractEidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
            Guid.TryParse(employeeIdClaim, out var employeeId);
            if (string.IsNullOrWhiteSpace(companyIdClaim) || !Guid.TryParse(companyIdClaim, out companyId))
            {
                return Unauthorized("Missing or invalid session cookie. Please log in again.");
            }
            var entityId = CreateDeterministicEntityId(request.Production, companyId);
            var ensured = await EnsureEntitiesAsync(companyId, cancellationToken);
            var session = request.ToTrackingSession(entityId);
            await _repository.InsertSessionAsync(session, cancellationToken);   
            return CreatedAtAction(nameof(Get), new { entityId = entityId, id = session.SessionId }, session);
        }
        return Ok();
    }

    [HttpDelete("{sessionId:guid}")]
    public async Task<IActionResult> Delete(Guid sessionId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteSessionCascadeAsync(sessionId, cancellationToken);
        return NoContent();
    }

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };

    private async Task<IReadOnlyCollection<MainEntity>> EnsureEntitiesAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        var productions = new[] { "PT", "PY", "FD" };
        var ensured = new List<MainEntity>();
        var entityRequest = new CreateMainEntityRequest(){CompanyId=companyId};
        var ensureEntity = await _repository.GetMainEntityByCompanyAsync(companyId,cancellationToken);
        if(ensureEntity is not null)
        {
            
        }
        foreach (var production in productions)
        {
            var ensuredId = CreateDeterministicEntityId(production, companyId);
            var existing = await _repository.GetMainEntityByIdAsync(ensuredId, cancellationToken);
            if (existing is not null)
            {
                ensured.Add(existing);
                continue;
            }

            var mainEntity = new MainEntity
            {
                EntityId = ensuredId,
                Production = production,
                CompanyId = companyId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var entity = entityRequest.ToMainEntity(mainEntity);
            ensured.Add(entity);
            await _repository.InsertMainEntityAsync(entity, cancellationToken);
        }

        return ensured;
    }

    private static Guid CreateDeterministicEntityId(string production, Guid cid)
    {
        var key = $"{production}:{cid}".ToLowerInvariant();
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

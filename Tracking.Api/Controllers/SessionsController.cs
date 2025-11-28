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


    [HttpPost("/sessions/{sessionId:guid}")]
    public async Task<ActionResult<TrackingSession>> Create([FromRoute] Guid? sessionId, [FromBody] CreateTrackingSessionRequest request, CancellationToken cancellationToken = default)
    {
        var targetEntity = sessionId.HasValue?sessionId:null;

        if (targetEntity is null)
        {
            var companyIdClaim = ExtractCidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
            var employeeId = ExtractEidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
            if (string.IsNullOrWhiteSpace(companyIdClaim) || !Guid.TryParse(companyIdClaim, out var companyId))
            {
                return Unauthorized("Missing or invalid session cookie. Please log in again.");
            }
            var entity = CreateDeterministicEntityId(request.Production, companyId);
            var ensured = await EnsureEntitiesAsync(companyId, entity, cancellationToken);
            targetEntity = ChooseEntity(sessionId, ensured);
        }

        var session = request.ToTrackingSession(targetEntity.EntityId);
        await _repository.InsertSessionAsync(session, cancellationToken);
        return CreatedAtAction(nameof(Get), new { entityId = targetEntity.EntityId, id = session.SessionId }, session);
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

    private static MainEntity ChooseEntity(Guid? requestedEntityId, IReadOnlyCollection<MainEntity> ensured)
    {
        if (requestedEntityId.HasValue)
        {
            var matched = ensured.FirstOrDefault(e => e.EntityId == requestedEntityId.Value);
            if (matched is not null)
            {
                return matched;
            }
        }

        var primary = ensured.FirstOrDefault(e => string.Equals(e.Production, "PT", StringComparison.OrdinalIgnoreCase));
        return primary ?? ensured.First();
    }

    private async Task<IReadOnlyCollection<MainEntity>> EnsureEntitiesAsync(
        Guid companyId,
        CreateMainEntityRequest? request,
        CancellationToken cancellationToken)
    {
        var productions = new[] { "PT", "PY", "FD" };
        var ensured = new List<MainEntity>();
        var entityRequest = request ?? CreateDefaultEntityRequest(companyId);
        entityRequest.CompanyId = companyId;

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
                CreatorId = entityRequest.CreatorId,
                CreatorEmail = entityRequest.CreatorEmail,
                Panels = entityRequest.Panels,
                Collaborators = entityRequest.Collaborators,
                Visibility = entityRequest.Visibility,
                IsShared = entityRequest.IsShared,
                SharedToken = entityRequest.SharedToken ?? Guid.NewGuid(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var entity = entityRequest.ToMainEntity(mainEntity);
            ensured.Add(entity);
            await _repository.InsertMainEntityAsync(entity, cancellationToken);
        }

        return ensured;
    }

    private static CreateMainEntityRequest CreateDefaultEntityRequest(Guid companyId) => new()
    {
        CompanyId = companyId,
        CreatorId = 0,
        CreatorEmail = "system@tracking.local",
        Panels = "{}",
        Collaborators = "[]",
        Visibility = "private",
        IsShared = false,
        SharedToken = null
    };

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

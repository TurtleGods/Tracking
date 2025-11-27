using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;
using System.Security.Cryptography;
using System.Text;
using System.IdentityModel.Tokens.Jwt;

namespace Tracking.Api.Controllers;

[ApiController]
[Route("entities")]
public sealed class EntitiesController : ControllerBase
{
    private readonly ITrackingRepository _repository;

    public EntitiesController(ITrackingRepository repository)
    {
        _repository = repository;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<MainEntity>>> Get([FromQuery] int limit = 50, CancellationToken cancellationToken = default)
    {
        var take = NormalizeLimit(limit);
        var entities = await _repository.GetMainEntitiesAsync(take, cancellationToken);
        return Ok(entities);
    }

    [HttpPost]
    public async Task<ActionResult<MainEntity>> Create([FromBody] CreateMainEntityRequest request, CancellationToken cancellationToken = default)
    {
        var company_id = ExtractCidFromCookie(Request.Cookies["__ModuleSessionCookie"]);
        if (string.IsNullOrWhiteSpace(company_id))
        {
            return Unauthorized("Missing or invalid session cookie. Please log in again.");
        }

        var productions = new[] { "PT", "PY", "FD" };
        var ensured = new List<MainEntity>();

        foreach (var production in productions)
        {
            var entityId = CreateDeterministicEntityId(production, company_id);
            var existing = await _repository.GetMainEntityByIdAsync(entityId, cancellationToken);
            if (existing is not null)
            {
                ensured.Add(existing);
                continue;
            }
            var mainEntity = new MainEntity()
            {
                EntityId = entityId,
                Production = production,
                CompanyId=new Guid(company_id),
            };
            var entity = request.ToMainEntity(mainEntity);
            ensured.Add(entity);
            await _repository.InsertMainEntityAsync(entity, cancellationToken);
        }

        return Ok(ensured);
    }

    [HttpGet("{entityId:guid}")]
    public async Task<ActionResult<MainEntity>> GetById([FromRoute] Guid entityId, CancellationToken cancellationToken = default)
    {
        var entities = await _repository.GetMainEntitiesAsync(500, cancellationToken);
        var entity = entities.FirstOrDefault(e => e.EntityId == entityId);
        return entity is null ? NotFound() : Ok(entity);
    }

    [HttpDelete("{entityId:guid}")]
    public async Task<IActionResult> Delete([FromRoute] Guid entityId, CancellationToken cancellationToken = default)
    {
        await _repository.DeleteEntityCascadeAsync(entityId, cancellationToken);
        return NoContent();
    }

    private static Guid CreateDeterministicEntityId(string production, string cid)
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

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };
}

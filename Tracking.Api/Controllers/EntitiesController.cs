using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Data;
using Tracking.Api.Models;

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

    private static int NormalizeLimit(int limit) => limit switch
    {
        < 1 => 50,
        > 500 => 500,
        _ => limit
    };
}

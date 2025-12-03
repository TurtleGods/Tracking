using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Controllers;
using Tracking.Api.Models;
using Tracking.Api.Tests.TestDoubles;

namespace Tracking.Api.Tests;

public sealed class EntitiesAndSessionsControllerTests
{
    [Fact]
    public async Task Entities_Get_NormalizesLimitAndReturnsEntities()
    {
        var repository = new FakeTrackingRepository();
        repository.MainEntities.Add(new MainEntity
        {
            EntityId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Production = "PT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var controller = new EntitiesController(repository);

        var result = await controller.Get(limit: 0, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entities = Assert.IsAssignableFrom<IEnumerable<MainEntity>>(ok.Value);
        Assert.Single(entities);
        Assert.Equal(50, repository.LastEntitiesLimit);
    }

    [Fact]
    public async Task Entities_Get_PassesThroughLimitWithinRange()
    {
        var repository = new FakeTrackingRepository();
        var controller = new EntitiesController(repository);

        var result = await controller.Get(limit: 100, cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(100, repository.LastEntitiesLimit);
    }

    [Fact]
    public async Task Entities_Get_NormalizesLimitAboveMax()
    {
        var repository = new FakeTrackingRepository();
        repository.MainEntities.Add(new MainEntity
        {
            EntityId = Guid.NewGuid(),
            CompanyId = Guid.NewGuid(),
            Production = "PT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var controller = new EntitiesController(repository);

        var result = await controller.Get(limit: 600, cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(500, repository.LastEntitiesLimit);
    }

    [Fact]
    public async Task Entities_GetById_NotFoundWhenMissing()
    {
        var repository = new FakeTrackingRepository();
        var controller = new EntitiesController(repository);

        var result = await controller.GetById(Guid.NewGuid(), CancellationToken.None);

        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task Entities_GetById_ReturnsEntityWhenPresent()
    {
        var repository = new FakeTrackingRepository();
        var targetId = Guid.NewGuid();
        repository.MainEntities.Add(new MainEntity
        {
            EntityId = targetId,
            CompanyId = Guid.NewGuid(),
            Production = "PT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        var controller = new EntitiesController(repository);

        var result = await controller.GetById(targetId, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var entity = Assert.IsType<MainEntity>(ok.Value);
        Assert.Equal(targetId, entity.EntityId);
    }

    [Fact]
    public async Task Entities_Delete_DelegatesToRepository()
    {
        var repository = new FakeTrackingRepository();
        var controller = new EntitiesController(repository);
        var entityId = Guid.NewGuid();

        var result = await controller.Delete(entityId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, repository.DeleteEntityCalls);
    }

    [Fact]
    public async Task Sessions_Get_NormalizesLimitAndReturnsSessions()
    {
        var repository = new FakeTrackingRepository();
        var entityId = Guid.NewGuid();
        repository.Sessions.Add(new TrackingSession
        {
            SessionId = Guid.NewGuid(),
            EntityId = entityId,
            CompanyId = Guid.NewGuid(),
            EmployeeId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });
        var controller = new SessionsController(repository);

        var result = await controller.Get(entityId, limit: 600, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var sessions = Assert.IsAssignableFrom<IEnumerable<TrackingSession>>(ok.Value);
        Assert.Single(sessions);
        Assert.Equal(500, repository.LastSessionsLimit);
    }

    [Fact]
    public async Task Sessions_Get_PassesThroughLimitWithinRange()
    {
        var repository = new FakeTrackingRepository();
        var entityId = Guid.NewGuid();
        var controller = new SessionsController(repository);

        var result = await controller.Get(entityId, limit: 100, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(100, repository.LastSessionsLimit);
    }

    [Fact]
    public async Task Sessions_Get_NormalizesLimitBelowOne()
    {
        var repository = new FakeTrackingRepository();
        var entityId = Guid.NewGuid();
        var controller = new SessionsController(repository);

        var result = await controller.Get(entityId, limit: -5, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(50, repository.LastSessionsLimit);
    }

    [Fact]
    public async Task Sessions_Delete_DelegatesToRepository()
    {
        var repository = new FakeTrackingRepository();
        var controller = new SessionsController(repository);
        var sessionId = Guid.NewGuid();

        var result = await controller.Delete(sessionId, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.Equal(1, repository.DeleteSessionCalls);
    }
}

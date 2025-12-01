using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Tracking.Api.Controllers;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;
using Tracking.Api.Tests.TestDoubles;

namespace Tracking.Api.Tests;

public sealed class EventsControllerTests
{
    [Fact]
    public async Task Create_NewSession_InsertsEntitiesForConfiguredProductions()
    {
        var repository = new FakeTrackingRepository();
        var options = Options.Create(new ProductionOptions { Codes = new[] { "PX", "QY" } });
        var controller = BuildController(repository, options, includeCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "click",
            EventName = "test",
            Production = "PX"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);
        var trackingEvent = Assert.IsType<TrackingEvent>(created.Value);

        Assert.Equal(2, repository.MainEntities.Count);
        Assert.Contains(repository.MainEntities, e => e.Production == "PX");
        Assert.Contains(repository.MainEntities, e => e.Production == "QY");
        Assert.Single(repository.Sessions);
        Assert.Single(repository.Events);
        Assert.Equal(repository.Sessions.Single().SessionId, trackingEvent.SessionId);
    }

    [Fact]
    public async Task Create_MissingCookie_ReturnsUnauthorized()
    {
        var repository = new FakeTrackingRepository();
        var options = Options.Create(new ProductionOptions { Codes = new[] { "PT" } });
        var controller = BuildController(repository, options, includeCookie: false);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("Missing or invalid session cookie. Please log in again.", unauthorized.Value);
        Assert.Empty(repository.MainEntities);
        Assert.Empty(repository.Sessions);
        Assert.Empty(repository.Events);
    }

    [Fact]
    public async Task Create_InvalidCookie_ReturnsUnauthorized()
    {
        var repository = new FakeTrackingRepository();
        var options = Options.Create(new ProductionOptions { Codes = new[] { "PT" } });
        var controller = BuildController(repository, options, includeCookie: true, invalidCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result.Result);
        Assert.Equal("Missing or invalid session cookie. Please log in again.", unauthorized.Value);
        Assert.Empty(repository.MainEntities);
    }

    [Fact]
    public async Task Create_UsesDefaultProductionCodesWhenOptionsEmpty()
    {
        var repository = new FakeTrackingRepository();
        var options = Options.Create(new ProductionOptions { Codes = Array.Empty<string>() });
        var controller = BuildController(repository, options, includeCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "click",
            EventName = "cta",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Equal(3, repository.MainEntities.Count);
        Assert.Contains(repository.MainEntities, e => e.Production == "PT");
        Assert.Contains(repository.MainEntities, e => e.Production == "PY");
        Assert.Contains(repository.MainEntities, e => e.Production == "FD");
    }

    [Fact]
    public async Task Create_ExistingSession_ReusesSessionAndDoesNotCreateEntities()
    {
        var repository = new FakeTrackingRepository();
        var companyId = Guid.NewGuid();
        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        repository.MainEntities.Add(new MainEntity
        {
            EntityId = entityId,
            CompanyId = companyId,
            Production = "PT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });
        repository.Sessions.Add(new TrackingSession
        {
            SessionId = sessionId,
            EntityId = entityId,
            CompanyId = companyId,
            EmployeeId = Guid.NewGuid(),
            StartedAt = DateTime.UtcNow,
            LastActivityAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        });

        var options = Options.Create(new ProductionOptions { Codes = new[] { "PT" } });
        var controller = BuildController(repository, options, includeCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "existing",
            Production = "PT"
        };

        var result = await controller.Create(sessionId, request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(repository.MainEntities);
        Assert.Single(repository.Sessions);
        Assert.Single(repository.Events);
        Assert.Equal(sessionId, repository.Events.Single().SessionId);
    }

    [Fact]
    public async Task Get_NormalizesLimitBelowOneToDefault()
    {
        var repository = new FakeTrackingRepository();
        var controller = BuildController(repository, Options.Create(new ProductionOptions { Codes = new[] { "PT" } }), includeCookie: true);

        var result = await controller.Get(Guid.NewGuid(), limit: 0, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(50, repository.LastEventsLimit);
    }

    [Fact]
    public async Task Get_NormalizesLimitAndReturnsEvents()
    {
        var repository = new FakeTrackingRepository();
        var entityId = Guid.NewGuid();
        var sessionId = Guid.NewGuid();
        var companyId = Guid.NewGuid();
        repository.Events.Add(new TrackingEvent
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            SessionId = sessionId,
            CompanyId = companyId,
            EmployeeId = Guid.NewGuid(),
            EventType = "click",
            EventName = "cta",
            Timestamp = DateTime.UtcNow
        });

        var controller = BuildController(repository, Options.Create(new ProductionOptions { Codes = new[] { "PT" } }), includeCookie: true);

        var result = await controller.Get(sessionId, limit: 600, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<TrackingEvent>>(ok.Value);
        Assert.Single(events);
        Assert.Equal(500, repository.LastEventsLimit);
    }

    [Fact]
    public async Task Get_UsesRequestedLimitWhenWithinRange()
    {
        var repository = new FakeTrackingRepository();
        var sessionId = Guid.NewGuid();
        var controller = BuildController(repository, Options.Create(new ProductionOptions { Codes = new[] { "PT" } }), includeCookie: true);

        var result = await controller.Get(sessionId, limit: 100, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(100, repository.LastEventsLimit);
    }

    [Fact]
    public async Task Create_UsesExistingEntityWhenFound()
    {
        var repository = new FakeTrackingRepository();
        var companyId = Guid.NewGuid();
        var employeeId = Guid.NewGuid();
        repository.MainEntities.Add(new MainEntity
        {
            EntityId = Guid.NewGuid(),
            CompanyId = companyId,
            Production = "PT",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var controller = BuildController(repository, Options.Create(new ProductionOptions { Codes = new[] { "PT" } }), includeCookie: true, invalidCookie: false, companyId: companyId, employeeId: employeeId);
        var request = new CreateTrackingEventRequest
        {
            EventType = "click",
            EventName = "existing-entity",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        Assert.IsType<CreatedAtActionResult>(result.Result);
        Assert.Single(repository.MainEntities);
    }

    private static EventsController BuildController(ITrackingRepository repository, IOptions<ProductionOptions> options, bool includeCookie, bool invalidCookie = false, Guid? companyId = null, Guid? employeeId = null)
    {
        var controller = new EventsController(repository, options);
        var httpContext = new DefaultHttpContext();
        if (includeCookie)
        {
            var cid = (companyId ?? Guid.NewGuid()).ToString();
            var eid = (employeeId ?? Guid.NewGuid()).ToString();
            var token = invalidCookie ? "not-a-jwt" : CreateCookieToken(cid, eid);
            httpContext.Request.Headers["Cookie"] = $"__ModuleSessionCookie={token}";
        }

        controller.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
        return controller;
    }

    private static string CreateCookieToken(string companyId, string employeeId)
    {
        var jwt = new JwtSecurityToken(claims: new[]
        {
            new Claim("cid", companyId),
            new Claim("eid", employeeId)
        });

        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}

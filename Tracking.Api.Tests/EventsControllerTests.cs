using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Controllers;
using Tracking.Api.Data;
using Tracking.Api.Models;
using Tracking.Api.Requests;
using Tracking.Api.Services;
using Tracking.Api.Tests.TestDoubles;

namespace Tracking.Api.Tests;

public sealed class EventsControllerTests
{
    [Fact]
    public async Task Create_EnqueuesCommand_ReturnsAccepted()
    {
        var repository = new FakeTrackingRepository();
        var queue = new FakeTrackingEventQueue();
        var controller = BuildController(repository, queue, includeCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "click",
            EventName = "test",
            Production = "PX"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var accepted = Assert.IsType<AcceptedResult>(result);
        Assert.Single(queue.Commands);
        var command = queue.Commands.Single();
        Assert.Equal(request.EventName, command.Request.EventName);
        Assert.Null(command.SessionId);
    }

    [Fact]
    public async Task Create_MissingCookie_ReturnsUnauthorized()
    {
        var repository = new FakeTrackingRepository();
        var queue = new FakeTrackingEventQueue();
        var controller = BuildController(repository, queue, includeCookie: false);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Missing or invalid session cookie. Please log in again.", unauthorized.Value);
        Assert.Empty(queue.Commands);
    }

    [Fact]
    public async Task Create_QueueFull_Returns429()
    {
        var repository = new FakeTrackingRepository();
        var queue = new FakeTrackingEventQueue { ShouldReject = true };
        var controller = BuildController(repository, queue, includeCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var tooMany = Assert.IsType<ObjectResult>(result);
        Assert.Equal(StatusCodes.Status429TooManyRequests, tooMany.StatusCode);
        Assert.Empty(queue.Commands);
    }

    [Fact]
    public async Task Create_WithSessionId_EnqueuesSession()
    {
        var repository = new FakeTrackingRepository();
        var queue = new FakeTrackingEventQueue();
        var controller = BuildController(repository, queue, includeCookie: true);
        var sessionId = Guid.NewGuid();
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(sessionId, request, CancellationToken.None);

        Assert.IsType<AcceptedResult>(result);
        var command = Assert.Single(queue.Commands);
        Assert.Equal(sessionId, command.SessionId);
    }

    [Fact]
    public async Task Create_InvalidCookie_ReturnsUnauthorized()
    {
        var repository = new FakeTrackingRepository();
        var queue = new FakeTrackingEventQueue();
        var controller = BuildController(repository, queue, includeCookie: true, invalidCookie: true);
        var request = new CreateTrackingEventRequest
        {
            EventType = "view",
            EventName = "landing",
            Production = "PT"
        };

        var result = await controller.Create(null, request, CancellationToken.None);

        var unauthorized = Assert.IsType<UnauthorizedObjectResult>(result);
        Assert.Equal("Missing or invalid session cookie. Please log in again.", unauthorized.Value);
        Assert.Empty(queue.Commands);
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

        var controller = BuildController(repository, new FakeTrackingEventQueue(), includeCookie: true);

        var result = await controller.Get(sessionId, limit: 600, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var events = Assert.IsAssignableFrom<IEnumerable<TrackingEvent>>(ok.Value);
        Assert.Single(events);
        Assert.Equal(500, repository.LastEventsLimit);
    }

    [Fact]
    public async Task Get_NormalizesLimitBelowOneToDefault()
    {
        var repository = new FakeTrackingRepository();
        var controller = BuildController(repository, new FakeTrackingEventQueue(), includeCookie: true);

        var result = await controller.Get(Guid.NewGuid(), limit: 0, CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(50, repository.LastEventsLimit);
    }

    private static EventsController BuildController(ITrackingRepository repository, ITrackingEventQueue queue, bool includeCookie, bool invalidCookie = false)
    {
        var controller = new EventsController(repository, queue);
        var httpContext = new DefaultHttpContext();
        if (includeCookie)
        {
            var companyId = Guid.NewGuid().ToString();
            var employeeId = Guid.NewGuid().ToString();
            var token = invalidCookie ? "not-a-jwt" : CreateCookieToken(companyId, employeeId);
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

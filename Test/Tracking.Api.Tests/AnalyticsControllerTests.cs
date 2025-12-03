using Microsoft.AspNetCore.Mvc;
using Tracking.Api.Controllers;
using Tracking.Api.Models;
using Tracking.Api.Tests.TestDoubles;

namespace Tracking.Api.Tests;

public sealed class AnalyticsControllerTests
{
    [Fact]
    public async Task Overview_UsesProvidedDateAndReturnsDailyMetrics()
    {
        var repository = new FakeTrackingRepository();
        var targetDate = new DateTime(2024, 01, 02, 15, 30, 0, DateTimeKind.Utc);
        var dayStart = DateTime.SpecifyKind(targetDate.Date, DateTimeKind.Utc);
        var entityId = Guid.NewGuid();
        var companyA = Guid.NewGuid();
        var companyB = Guid.NewGuid();
        var employeeA = Guid.NewGuid();
        var employeeB = Guid.NewGuid();
        var sessionId = Guid.NewGuid();

        repository.Events.AddRange(new[]
        {
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = sessionId,
                CompanyId = companyA,
                EmployeeId = employeeA,
                EventType = "click",
                EventName = "cta",
                Timestamp = dayStart.AddHours(1)
            },
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = companyB,
                EmployeeId = employeeB,
                EventType = "view",
                EventName = "landing",
                Timestamp = dayStart.AddHours(2)
            },
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = companyB,
                EmployeeId = employeeB,
                EventType = "click",
                EventName = "old",
                Timestamp = dayStart.AddDays(-1)
            }
        });

        repository.Sessions.AddRange(new[]
        {
            new TrackingSession
            {
                SessionId = sessionId,
                EntityId = entityId,
                CompanyId = companyA,
                EmployeeId = employeeA,
                StartedAt = dayStart.AddMinutes(5),
                LastActivityAt = dayStart.AddHours(1),
                CreatedAt = dayStart.AddMinutes(5)
            },
            new TrackingSession
            {
                SessionId = Guid.NewGuid(),
                EntityId = entityId,
                CompanyId = companyA,
                EmployeeId = employeeA,
                StartedAt = dayStart.AddDays(1),
                LastActivityAt = dayStart.AddDays(1).AddHours(1),
                CreatedAt = dayStart.AddDays(1)
            }
        });

        var controller = new AnalyticsController(repository);

        var result = await controller.GetOverview(targetDate, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<DailyOverviewMetrics>(ok.Value);
        Assert.Equal(dayStart, metrics.DateUtc);
        Assert.Equal<ulong>(2, metrics.DailyActiveUsers);
        Assert.Equal<ulong>(2, metrics.ActiveCompanies);
        Assert.Equal<ulong>(2, metrics.TotalEvents);
        Assert.Equal<ulong>(1, metrics.Sessions);
        Assert.Equal(dayStart, repository.LastOverviewDateUtc);
    }

    [Fact]
    public async Task Overview_DefaultsToUtcTodayWhenDateMissing()
    {
        var repository = new FakeTrackingRepository();
        var controller = new AnalyticsController(repository);

        var result = await controller.GetOverview(null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var metrics = Assert.IsType<DailyOverviewMetrics>(ok.Value);
        var expectedDay = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        Assert.Equal(expectedDay, metrics.DateUtc);
        Assert.Equal(expectedDay, repository.LastOverviewDateUtc);
    }
}

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

    [Fact]
    public async Task EventVolume_DefaultsTo24hHourlyBucketsAndPassesFilters()
    {
        var repository = new FakeTrackingRepository();
        var endUtc = new DateTime(2024, 02, 08, 12, 0, 0, DateTimeKind.Utc);
        repository.EventVolumePoints.Add(new EventVolumePoint
        {
            BucketStartUtc = endUtc.AddHours(-1),
            Events = 10
        });
        var controller = new AnalyticsController(repository);

        var result = await controller.GetEventVolume(range: "24h", eventType: "click", production: "PT", endUtc: endUtc, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var points = Assert.IsAssignableFrom<IEnumerable<EventVolumePoint>>(ok.Value);
        Assert.Single(points);
        Assert.Equal(TimeSpan.FromHours(1), repository.LastVolumeBucket);
        Assert.Equal(endUtc.AddHours(-24), repository.LastVolumeStartUtc);
        Assert.Equal(endUtc, repository.LastVolumeEndUtc);
        Assert.Equal("click", repository.LastVolumeEventType);
        Assert.Equal("PT", repository.LastVolumeProduction);
    }

    [Fact]
    public async Task EventVolume_Uses7DayRangeWithDailyBuckets()
    {
        var repository = new FakeTrackingRepository();
        var endUtc = new DateTime(2024, 03, 10, 0, 0, 0, DateTimeKind.Utc);
        var controller = new AnalyticsController(repository);

        var result = await controller.GetEventVolume(range: "7d", eventType: null, production: "PX", endUtc: endUtc, cancellationToken: CancellationToken.None);

        Assert.IsType<OkObjectResult>(result.Result);
        Assert.Equal(TimeSpan.FromDays(1), repository.LastVolumeBucket);
        Assert.Equal(endUtc.AddDays(-7), repository.LastVolumeStartUtc);
        Assert.Equal(endUtc, repository.LastVolumeEndUtc);
        Assert.Null(repository.LastVolumeEventType);
        Assert.Equal("PX", repository.LastVolumeProduction);
    }

    [Fact]
    public async Task FeatureUsage_ComputesCountsAndPercentages()
    {
        var repository = new FakeTrackingRepository();
        var entityId = Guid.NewGuid();
        repository.MainEntities.Add(new MainEntity
        {
            EntityId = entityId,
            CompanyId = Guid.NewGuid(),
            Production = "PX",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        });

        var endUtc = new DateTime(2024, 04, 08, 0, 0, 0, DateTimeKind.Utc);
        var windowStart = endUtc.AddDays(-7);
        repository.Events.AddRange(new[]
        {
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = repository.MainEntities[0].CompanyId,
                EmployeeId = Guid.NewGuid(),
                EventType = "click",
                EventName = "cta",
                Timestamp = windowStart.AddDays(1)
            },
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = repository.MainEntities[0].CompanyId,
                EmployeeId = Guid.NewGuid(),
                EventType = "click",
                EventName = "modal_open",
                Timestamp = windowStart.AddDays(2)
            },
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = repository.MainEntities[0].CompanyId,
                EmployeeId = Guid.NewGuid(),
                EventType = "view",
                EventName = "ignored_by_type",
                Timestamp = windowStart.AddDays(3)
            },
            new TrackingEvent
            {
                Id = Guid.NewGuid(),
                EntityId = entityId,
                SessionId = Guid.NewGuid(),
                CompanyId = repository.MainEntities[0].CompanyId,
                EmployeeId = Guid.NewGuid(),
                EventType = "click",
                EventName = "cta",
                Timestamp = endUtc.AddDays(1)
            }
        });

        var controller = new AnalyticsController(repository);

        var result = await controller.GetFeatureUsage(range: "7d", eventType: "click", production: "PX", endUtc: endUtc, cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var usage = Assert.IsAssignableFrom<IEnumerable<FeatureUsage>>(ok.Value);
        var list = usage.ToList();

        Assert.Equal(windowStart, repository.LastUsageStartUtc);
        Assert.Equal(endUtc, repository.LastUsageEndUtc);
        Assert.Equal("click", repository.LastUsageEventType);
        Assert.Equal("PX", repository.LastUsageProduction);

        Assert.Equal(2, list.Count);
        var cta = Assert.Single(list, u => u.EventName == "cta");
        Assert.Equal<ulong>(1, cta.Count);
        Assert.Equal(50.0, cta.Percentage);
        var modal = Assert.Single(list, u => u.EventName == "modal_open");
        Assert.Equal<ulong>(1, modal.Count);
        Assert.Equal(50.0, modal.Percentage);
    }
}

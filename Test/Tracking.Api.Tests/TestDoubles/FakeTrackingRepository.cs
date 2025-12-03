using Tracking.Api.Data;
using Tracking.Api.Models;

namespace Tracking.Api.Tests.TestDoubles;

public sealed class FakeTrackingRepository : ITrackingRepository
{
    public List<MainEntity> MainEntities { get; } = new();
    public List<TrackingSession> Sessions { get; } = new();
    public List<TrackingEvent> Events { get; } = new();

    public int LastEntitiesLimit { get; private set; }
    public int LastSessionsLimit { get; private set; }
    public int LastEventsLimit { get; private set; }
    public DateTime? LastOverviewDateUtc { get; private set; }
    public int DeleteEntityCalls { get; private set; }
    public int DeleteSessionCalls { get; private set; }

    public Task<IEnumerable<MainEntity>> GetMainEntitiesAsync(int limit, CancellationToken cancellationToken)
    {
        LastEntitiesLimit = limit;
        var results = MainEntities.Take(limit);
        return Task.FromResult(results);
    }

    public Task<MainEntity?> GetMainEntityByCompanyAndProductionAsync(Guid companyId, string production, CancellationToken cancellationToken)
    {
        var entity = MainEntities.FirstOrDefault(e => e.CompanyId == companyId && e.Production == production);
        return Task.FromResult(entity);
    }

    public Task<MainEntity?> GetMainEntityByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        var entity = MainEntities.FirstOrDefault(e => e.EntityId == entityId);
        return Task.FromResult(entity);
    }

    public Task InsertMainEntityAsync(MainEntity entity, CancellationToken cancellationToken)
    {
        MainEntities.RemoveAll(e => e.EntityId == entity.EntityId);
        MainEntities.Add(entity);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TrackingSession>> GetSessionsAsync(Guid entityId, int limit, CancellationToken cancellationToken)
    {
        LastSessionsLimit = limit;
        var sessions = Sessions.Where(s => s.EntityId == entityId).Take(limit);
        return Task.FromResult(sessions);
    }

    public Task<TrackingSession?> GetEventBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        var session = Sessions.FirstOrDefault(s => s.SessionId == sessionId);
        return Task.FromResult(session);
    }

    public Task InsertSessionAsync(TrackingSession session, CancellationToken cancellationToken)
    {
        Sessions.RemoveAll(s => s.SessionId == session.SessionId);
        Sessions.Add(session);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<TrackingEvent>> GetEventsBySessionAsync(Guid sessionId, int limit, CancellationToken cancellationToken)
    {
        LastEventsLimit = limit;
        var eventsForSession = Events.Where(e => e.SessionId == sessionId).Take(limit);
        return Task.FromResult(eventsForSession);
    }

    public Task InsertEventAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken)
    {
        Events.Add(trackingEvent);
        return Task.CompletedTask;
    }

    public Task<DailyOverviewMetrics> GetDailyOverviewAsync(DateTime dateUtc, CancellationToken cancellationToken)
    {
        var dayStart = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);
        LastOverviewDateUtc = dayStart;

        var eventsForDay = Events.Where(e => e.Timestamp >= dayStart && e.Timestamp < dayEnd).ToList();
        var dau = eventsForDay.Select(e => e.EmployeeId).Distinct().Count();
        var totalEvents = eventsForDay.Count;
        var activeCompanies = eventsForDay.Select(e => e.CompanyId).Distinct().Count();
        var sessions = Sessions.Count(s => s.StartedAt >= dayStart && s.StartedAt < dayEnd);

        var metrics = new DailyOverviewMetrics
        {
            DateUtc = dayStart,
            DailyActiveUsers = (ulong)dau,
            TotalEvents = (ulong)totalEvents,
            Sessions = (ulong)sessions,
            ActiveCompanies = (ulong)activeCompanies
        };

        return Task.FromResult(metrics);
    }

    public Task DeleteEntityCascadeAsync(Guid entityId, CancellationToken cancellationToken)
    {
        DeleteEntityCalls++;
        var sessionIds = Sessions.Where(s => s.EntityId == entityId).Select(s => s.SessionId).ToHashSet();
        Sessions.RemoveAll(s => s.EntityId == entityId);
        Events.RemoveAll(e => sessionIds.Contains(e.SessionId));
        MainEntities.RemoveAll(e => e.EntityId == entityId);
        return Task.CompletedTask;
    }

    public Task DeleteSessionCascadeAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        DeleteSessionCalls++;
        Sessions.RemoveAll(s => s.SessionId == sessionId);
        Events.RemoveAll(e => e.SessionId == sessionId);
        return Task.CompletedTask;
    }
}

using System.Diagnostics.CodeAnalysis;
using System.Data;
using System.Data.Common;
using ClickHouse.Client.ADO;
using Tracking.Api.Models;

namespace Tracking.Api.Data;

public interface ITrackingRepository
{
    Task<IEnumerable<MainEntity>> GetMainEntitiesAsync(int limit, CancellationToken cancellationToken);
    Task<MainEntity?> GetMainEntityByCompanyAndProductionAsync(Guid companyId, string production, CancellationToken cancellationToken);
    Task<MainEntity?> GetMainEntityByIdAsync(Guid entityId, CancellationToken cancellationToken);
    Task InsertMainEntityAsync(MainEntity entity, CancellationToken cancellationToken);
    Task<IEnumerable<TrackingSession>> GetSessionsAsync(Guid entityId, int limit, CancellationToken cancellationToken);
    Task<TrackingSession?> GetEventBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken);
    Task InsertSessionAsync(TrackingSession session, CancellationToken cancellationToken);
    Task<IEnumerable<TrackingEvent>> GetEventsBySessionAsync(Guid sessionId, int limit, CancellationToken cancellationToken);
    Task InsertEventAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken);
    Task<DailyOverviewMetrics> GetDailyOverviewAsync(DateTime dateUtc, CancellationToken cancellationToken);
    Task<IEnumerable<EventVolumePoint>> GetEventVolumeAsync(
        DateTime startUtc,
        DateTime endUtc,
        TimeSpan bucket,
        string? eventType,
        string? production,
        CancellationToken cancellationToken);
    Task<IEnumerable<FeatureUsage>> GetFeatureUsageAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? eventType,
        string? production,
        CancellationToken cancellationToken);
    Task<IEnumerable<UserActivationFunnelCount>> GetUserActivationFunnelAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? production,
        CancellationToken cancellationToken);
    Task DeleteEntityCascadeAsync(Guid entityId, CancellationToken cancellationToken);
    Task DeleteSessionCascadeAsync(Guid sessionId, CancellationToken cancellationToken);
}

[ExcludeFromCodeCoverage]
public sealed class ClickHouseTrackingRepository : ITrackingRepository
{
    private readonly ClickHouseConnectionFactory _connectionFactory;

    public ClickHouseTrackingRepository(ClickHouseConnectionFactory connectionFactory)
    {
        _connectionFactory = connectionFactory;
    }

    public async Task<IEnumerable<MainEntity>> GetMainEntitiesAsync(int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entity_id,
                   company_id,
                   production,
                   created_at,
                   updated_at,
                   deleted_at
            FROM main_entities
            ORDER BY created_at DESC
            LIMIT @limit;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "limit", DbType.Int32, limit);

        var entities = new List<MainEntity>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            entities.Add(new MainEntity
            {
                EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
                CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
                Production = reader.GetString(reader.GetOrdinal("production")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            });
        }

        return entities;
    }

    public async Task<MainEntity?> GetMainEntityByIdAsync(Guid entityId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entity_id,
                   company_id,
                   production,
                   created_at,
                   updated_at
            FROM main_entities
            WHERE entity_id = @entity_id
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "entity_id", DbType.Guid, entityId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MainEntity
        {
            EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
            CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
            Production = reader.GetString(reader.GetOrdinal("production")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task<MainEntity?> GetMainEntityByCompanyAndProductionAsync(Guid companyId,string production, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT entity_id,
                   company_id,
                   production,
                   created_at,
                   updated_at
            FROM main_entities
            WHERE company_id = @company_id AND production = @production
            ORDER BY created_at DESC
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "company_id", DbType.Guid, companyId);
        AddParameter(command, "production", DbType.String, production);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new MainEntity
        {
            EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
            CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
            Production = reader.GetString(reader.GetOrdinal("production")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
            UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
        };
    }

    public async Task InsertMainEntityAsync(MainEntity entity, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO main_entities
            (
                entity_id,
                company_id,
                production,
                created_at,
                updated_at
            )
            VALUES
            (
                @entity_id,
                @company_id,
                @production,
                @created_at,
                @updated_at
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddCommonParametersForEntity(command, entity);
        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<IEnumerable<TrackingSession>> GetSessionsAsync(Guid entityId, int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT session_id,
                   entity_id,
                   employee_id,
                   company_id,
                   started_at,
                   last_activity_at,
                   ended_at,
                   created_at
            FROM tracking_sessions
            WHERE entity_id = @entity_id
            ORDER BY started_at DESC
            LIMIT @limit;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "entity_id", DbType.Guid, entityId);
        AddParameter(command, "limit", DbType.Int32, limit);

        var sessions = new List<TrackingSession>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            sessions.Add(new TrackingSession
            {
                SessionId = reader.GetFieldValue<Guid>(reader.GetOrdinal("session_id")),
                EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
                EmployeeId = reader.GetFieldValue<Guid>(reader.GetOrdinal("employee_id")),
                CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
                StartedAt = reader.GetDateTime(reader.GetOrdinal("started_at")),
                LastActivityAt = reader.GetDateTime(reader.GetOrdinal("last_activity_at")),
                EndedAt = reader.IsDBNull(reader.GetOrdinal("ended_at"))
                    ? null
                    : reader.GetDateTime(reader.GetOrdinal("ended_at")),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
            });
        }

        return sessions;
    }

    public async Task<TrackingSession?> GetEventBySessionIdAsync(Guid sessionId, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT session_id,
                   entity_id,
                   employee_id,
                   company_id,
                   started_at,
                   last_activity_at,
                   ended_at,
                   created_at
            FROM tracking_sessions
            WHERE session_id = @session_id
            LIMIT 1;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "session_id", DbType.Guid, sessionId);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return null;
        }

        return new TrackingSession
        {
            SessionId = reader.GetFieldValue<Guid>(reader.GetOrdinal("session_id")),
            EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
            EmployeeId = reader.GetFieldValue<Guid>(reader.GetOrdinal("employee_id")),
            CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
            StartedAt = reader.GetDateTime(reader.GetOrdinal("started_at")),
            LastActivityAt = reader.GetDateTime(reader.GetOrdinal("last_activity_at")),
            EndedAt = reader.IsDBNull(reader.GetOrdinal("ended_at"))
                ? null
                : reader.GetDateTime(reader.GetOrdinal("ended_at")),
            CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at"))
        };
    }

    public async Task InsertSessionAsync(TrackingSession session, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tracking_sessions
            (
                session_id,
                entity_id,
                employee_id,
                company_id,
                started_at,
                last_activity_at,
                ended_at,
                created_at
            )
            VALUES
            (
                @session_id,
                @entity_id,
                @employee_id,
                @company_id,
                @started_at,
                @last_activity_at,
                @ended_at,
                @created_at
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "session_id", DbType.Guid, session.SessionId);
        AddParameter(command, "entity_id", DbType.Guid, session.EntityId);
        AddParameter(command, "employee_id", DbType.Guid, session.EmployeeId);
        AddParameter(command, "company_id", DbType.Guid, session.CompanyId);
        AddParameter(command, "started_at", DbType.DateTime2, session.StartedAt);
        AddParameter(command, "last_activity_at", DbType.DateTime2, session.LastActivityAt);
        AddParameter(command, "ended_at", DbType.DateTime2, (object?)session.EndedAt ?? DBNull.Value);
        AddParameter(command, "created_at", DbType.DateTime2, session.CreatedAt);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }



    public async Task<IEnumerable<TrackingEvent>> GetEventsBySessionAsync(Guid sessionId, int limit, CancellationToken cancellationToken)
    {
        const string sql = """
            SELECT id,
                   entity_id,
                   session_id,
                   event_type,
                   event_name,
                   page_name,
                   component_name,
                   timestamp,
                   refer,
                   expose_time,
                   employee_id,
                   company_id,
                   device_type,
                   os_version,
                   browser_version,
                   page_url,
                   page_title,
                   properties
            FROM tracking_events
            WHERE session_id = @session_id
            ORDER BY timestamp DESC
            LIMIT @limit;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "session_id", DbType.Guid, sessionId);
        AddParameter(command, "limit", DbType.Int32, limit);

        var events = new List<TrackingEvent>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            events.Add(new TrackingEvent
            {
                Id = reader.GetFieldValue<Guid>(reader.GetOrdinal("id")),
                EntityId = reader.GetFieldValue<Guid>(reader.GetOrdinal("entity_id")),
                SessionId = reader.GetFieldValue<Guid>(reader.GetOrdinal("session_id")),
                EventType = reader.GetString(reader.GetOrdinal("event_type")),
                EventName = reader.GetString(reader.GetOrdinal("event_name")),
                PageName = reader.GetString(reader.GetOrdinal("page_name")),
                ComponentName = reader.GetString(reader.GetOrdinal("component_name")),
                Timestamp = reader.GetDateTime(reader.GetOrdinal("timestamp")),
                Refer = reader.GetString(reader.GetOrdinal("refer")),
                ExposeTime = reader.GetInt32(reader.GetOrdinal("expose_time")),
                EmployeeId = reader.GetFieldValue<Guid>(reader.GetOrdinal("employee_id")),
                CompanyId = reader.GetFieldValue<Guid>(reader.GetOrdinal("company_id")),
                DeviceType = reader.GetString(reader.GetOrdinal("device_type")),
                OsVersion = reader.GetString(reader.GetOrdinal("os_version")),
                BrowserVersion = reader.GetString(reader.GetOrdinal("browser_version")),
                PageUrl = reader.GetString(reader.GetOrdinal("page_url")),
                PageTitle = reader.GetString(reader.GetOrdinal("page_title")),
                Properties = reader.GetString(reader.GetOrdinal("properties"))
            });
        }

        return events;
    }

    public async Task InsertEventAsync(TrackingEvent trackingEvent, CancellationToken cancellationToken)
    {
        const string sql = """
            INSERT INTO tracking_events
            (
                id,
                entity_id,
                session_id,
                event_type,
                event_name,
                page_name,
                component_name,
                timestamp,
                refer,
                expose_time,
                employee_id,
                company_id,
                device_type,
                os_version,
                browser_version,
                page_url,
                page_title,
                properties
            )
            VALUES
            (
                @id,
                @entity_id,
                @session_id,
                @event_type,
                @event_name,
                @page_name,
                @component_name,
                @timestamp,
                @refer,
                @expose_time,
                @employee_id,
                @company_id,
                @device_type,
                @os_version,
                @browser_version,
                @page_url,
                @page_title,
                @properties
            );
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "id", DbType.Guid, trackingEvent.Id);
        AddParameter(command, "entity_id", DbType.Guid, trackingEvent.EntityId);
        AddParameter(command, "session_id", DbType.Guid, trackingEvent.SessionId);
        AddParameter(command, "event_type", DbType.String, trackingEvent.EventType);
        AddParameter(command, "event_name", DbType.String, trackingEvent.EventName);
        AddParameter(command, "page_name", DbType.String, trackingEvent.PageName);
        AddParameter(command, "component_name", DbType.String, trackingEvent.ComponentName);
        AddParameter(command, "timestamp", DbType.DateTime2, trackingEvent.Timestamp);
        AddParameter(command, "refer", DbType.String, trackingEvent.Refer);
        AddParameter(command, "expose_time", DbType.Int32, trackingEvent.ExposeTime);
        AddParameter(command, "employee_id", DbType.Guid, trackingEvent.EmployeeId);
        AddParameter(command, "company_id", DbType.Guid, trackingEvent.CompanyId);
        AddParameter(command, "device_type", DbType.String, trackingEvent.DeviceType);
        AddParameter(command, "os_version", DbType.String, trackingEvent.OsVersion);
        AddParameter(command, "browser_version", DbType.String, trackingEvent.BrowserVersion);
        AddParameter(command, "page_url", DbType.String, trackingEvent.PageUrl);
        AddParameter(command, "page_title", DbType.String, trackingEvent.PageTitle);
        AddParameter(command, "properties", DbType.String, trackingEvent.Properties);

        await command.ExecuteNonQueryAsync(cancellationToken);
    }

    public async Task<DailyOverviewMetrics> GetDailyOverviewAsync(DateTime dateUtc, CancellationToken cancellationToken)
    {
        var dayStart = DateTime.SpecifyKind(dateUtc.Date, DateTimeKind.Utc);
        var dayEnd = dayStart.AddDays(1);

        const string sql = """
            SELECT
                dau,
                total_events,
                sessions,
                active_companies
            FROM
            (
                SELECT
                    uniqExact(employee_id) AS dau,
                    count() AS total_events,
                    uniqExact(company_id) AS active_companies
                FROM tracking_events
                WHERE timestamp >= @start AND timestamp < @end
            ) AS events
            CROSS JOIN
            (
                SELECT count() AS sessions
                FROM tracking_sessions
                WHERE started_at >= @start AND started_at < @end
            ) AS sessions;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "start", DbType.DateTime2, dayStart);
        AddParameter(command, "end", DbType.DateTime2, dayEnd);

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        if (!await reader.ReadAsync(cancellationToken))
        {
            return new DailyOverviewMetrics
            {
                DateUtc = dayStart,
                DailyActiveUsers = 0,
                TotalEvents = 0,
                Sessions = 0,
                ActiveCompanies = 0
            };
        }

        return new DailyOverviewMetrics
        {
            DateUtc = dayStart,
            DailyActiveUsers = reader.GetFieldValue<ulong>(reader.GetOrdinal("dau")),
            TotalEvents = reader.GetFieldValue<ulong>(reader.GetOrdinal("total_events")),
            Sessions = reader.GetFieldValue<ulong>(reader.GetOrdinal("sessions")),
            ActiveCompanies = reader.GetFieldValue<ulong>(reader.GetOrdinal("active_companies"))
        };
    }

    public async Task<IEnumerable<EventVolumePoint>> GetEventVolumeAsync(
        DateTime startUtc,
        DateTime endUtc,
        TimeSpan bucket,
        string? eventType,
        string? production,
        CancellationToken cancellationToken)
    {
        if (bucket <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(bucket), "Bucket must be positive.");
        }

        var bucketSeconds = (long)bucket.TotalSeconds;
        var normalizedStart = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var normalizedEnd = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
        const string sql = """
            SELECT
                toStartOfInterval(e.timestamp, toIntervalSecond(@bucket_seconds)) AS bucket_start,
                count() AS events
            FROM tracking_events e
            LEFT JOIN main_entities m ON e.entity_id = m.entity_id
            WHERE e.timestamp >= @start
              AND e.timestamp < @end
              AND (@event_type = '' OR e.event_type = @event_type)
              AND (@production = '' OR m.production = @production)
            GROUP BY bucket_start
            ORDER BY bucket_start;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "bucket_seconds", DbType.Int64, bucketSeconds);
        AddParameter(command, "start", DbType.DateTime2, normalizedStart);
        AddParameter(command, "end", DbType.DateTime2, normalizedEnd);
        AddParameter(command, "event_type", DbType.String, eventType ?? string.Empty);
        AddParameter(command, "production", DbType.String, production ?? string.Empty);

        var points = new List<EventVolumePoint>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            points.Add(new EventVolumePoint
            {
                BucketStartUtc = reader.GetDateTime(reader.GetOrdinal("bucket_start")),
                Events = reader.GetFieldValue<ulong>(reader.GetOrdinal("events"))
            });
        }

        return points;
    }

    public async Task<IEnumerable<FeatureUsage>> GetFeatureUsageAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? eventType,
        string? production,
        CancellationToken cancellationToken)
    {
        var normalizedStart = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var normalizedEnd = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
        const string sql = """
            WITH
                (
                    SELECT count() AS total_events
                    FROM tracking_events e
                    LEFT JOIN main_entities m ON e.entity_id = m.entity_id
                    WHERE e.timestamp >= @start
                      AND e.timestamp < @end
                      AND (@event_type = '' OR e.event_type = @event_type)
                      AND (@production = '' OR m.production = @production)
                ) AS total_events
            SELECT
                e.event_name,
                count() AS events,
                if(total_events = 0, 0.0, round(100.0 * count() / total_events, 2)) AS percentage
            FROM tracking_events e
            LEFT JOIN main_entities m ON e.entity_id = m.entity_id
            WHERE e.timestamp >= @start
              AND e.timestamp < @end
              AND (@event_type = '' OR e.event_type = @event_type)
              AND (@production = '' OR m.production = @production)
            GROUP BY e.event_name, total_events
            ORDER BY events DESC, e.event_name ASC;
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "start", DbType.DateTime2, normalizedStart);
        AddParameter(command, "end", DbType.DateTime2, normalizedEnd);
        AddParameter(command, "event_type", DbType.String, eventType ?? string.Empty);
        AddParameter(command, "production", DbType.String, production ?? string.Empty);

        var usage = new List<FeatureUsage>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            usage.Add(new FeatureUsage
            {
                EventName = reader.GetString(reader.GetOrdinal("event_name")),
                Count = reader.GetFieldValue<ulong>(reader.GetOrdinal("events")),
                Percentage = reader.GetDouble(reader.GetOrdinal("percentage"))
            });
        }

        return usage;
    }

    public async Task<IEnumerable<UserActivationFunnelCount>> GetUserActivationFunnelAsync(
        DateTime startUtc,
        DateTime endUtc,
        string? production,
        CancellationToken cancellationToken)
    {
        var normalizedStart = DateTime.SpecifyKind(startUtc, DateTimeKind.Utc);
        var normalizedEnd = DateTime.SpecifyKind(endUtc, DateTimeKind.Utc);
        const string sql = """
            WITH sessions_in_window AS
            (
                SELECT s.session_id, s.entity_id
                FROM tracking_sessions s
                LEFT JOIN main_entities m ON s.entity_id = m.entity_id
                WHERE s.started_at >= @start
                  AND s.started_at < @end
                  AND (@production = '' OR m.production = @production)
            ),
            first_events AS
            (
                SELECT DISTINCT e.session_id
                FROM tracking_events e
                INNER JOIN sessions_in_window s ON e.session_id = s.session_id
                WHERE e.timestamp >= @start
                  AND e.timestamp < @end
                  AND lower(e.event_type) IN ('page_view', 'pageview', 'view', 'load', 'page_load')
            ),
            meaningful_events AS
            (
                SELECT DISTINCT e.session_id
                FROM tracking_events e
                INNER JOIN sessions_in_window s ON e.session_id = s.session_id
                WHERE e.timestamp >= @start
                  AND e.timestamp < @end
                  AND lower(e.event_type) = 'click'
            )
            SELECT stage, sessions
            FROM
            (
                SELECT 'session_start' AS stage, count() AS sessions FROM sessions_in_window
                UNION ALL
                SELECT 'first_event' AS stage, count() FROM first_events
                UNION ALL
                SELECT 'meaningful_event' AS stage, count() FROM meaningful_events
            )
            ORDER BY indexOf(['session_start', 'first_event', 'meaningful_event'], stage);
            """;

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        await using var command = CreateCommand(connection, sql);
        AddParameter(command, "start", DbType.DateTime2, normalizedStart);
        AddParameter(command, "end", DbType.DateTime2, normalizedEnd);
        AddParameter(command, "production", DbType.String, production ?? string.Empty);

        var steps = new List<UserActivationFunnelCount>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            steps.Add(new UserActivationFunnelCount
            {
                Stage = reader.GetString(reader.GetOrdinal("stage")),
                Sessions = reader.GetFieldValue<ulong>(reader.GetOrdinal("sessions"))
            });
        }

        return steps;
    }

    public async Task DeleteEntityCascadeAsync(Guid entityId, CancellationToken cancellationToken)
    {
        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);
        var deleteStatements = new[]
        {
            "ALTER TABLE tracking_events DELETE WHERE entity_id = @entity_id;",
            "ALTER TABLE tracking_sessions DELETE WHERE entity_id = @entity_id;",
            "ALTER TABLE dashboard_editors DELETE WHERE entity_id = @entity_id;",
            "ALTER TABLE dashboard_favorites DELETE WHERE entity_id = @entity_id;",
            "ALTER TABLE main_entities DELETE WHERE entity_id = @entity_id;"
        };

        foreach (var statement in deleteStatements)
        {
            await using var command = CreateCommand(connection, statement);
            AddParameter(command, "entity_id", DbType.Guid, entityId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    public async Task DeleteSessionCascadeAsync( Guid sessionId, CancellationToken cancellationToken)
    {
        const string deleteEvents = "ALTER TABLE tracking_events DELETE WHERE session_id = @session_id;";
        const string deleteSession = "ALTER TABLE tracking_sessions DELETE WHERE session_id = @session_id;";

        await using var connection = await _connectionFactory.CreateOpenConnectionAsync(cancellationToken);

        await using (var command = CreateCommand(connection, deleteEvents))
        {
            AddParameter(command, "session_id", DbType.Guid, sessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await using (var command = CreateCommand(connection, deleteSession))
        {
            AddParameter(command, "session_id", DbType.Guid, sessionId);
            await command.ExecuteNonQueryAsync(cancellationToken);
        }
    }

    private static void AddCommonParametersForEntity(DbCommand command, MainEntity entity)
    {
        AddParameter(command, "entity_id", DbType.Guid, entity.EntityId);
        AddParameter(command, "company_id", DbType.Guid, entity.CompanyId);
        AddParameter(command, "production", DbType.String, entity.Production);
        AddParameter(command, "created_at", DbType.DateTime2, entity.CreatedAt);
        AddParameter(command, "updated_at", DbType.DateTime2, entity.UpdatedAt);
    }

    private static ClickHouseCommand CreateCommand(ClickHouseConnection connection, string sql)
    {
        var command = connection.CreateCommand();
        command.CommandText = sql;
        return command;
    }

    private static void AddParameter(DbCommand command, string name, DbType dbType, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.DbType = dbType;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}

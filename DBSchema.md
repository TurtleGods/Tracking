# ClickHouse Schema (Production Ready)

Source of truth for the ClickHouse tables used by the API. Schema is applied
from `db/001_clickhouse_init.sql`.

## Tables

```sql
CREATE TABLE IF NOT EXISTS tracking.main_entities
(
    entity_id UUID,
    company_id UUID,
    production String,
    created_at DateTime64(3) DEFAULT now64(),
    updated_at DateTime64(3) DEFAULT now64(),
    deleted_at Nullable(DateTime64(3))
) ENGINE = ReplacingMergeTree
PARTITION BY toYYYYMM(created_at)
ORDER BY (entity_id, created_at)
TTL toDateTime(created_at) + INTERVAL 365 DAY DELETE;
```

```sql
CREATE TABLE IF NOT EXISTS tracking.tracking_sessions
(
    session_id UUID,
    entity_id UUID,
    employee_id UUID,
    company_id UUID,
    started_at DateTime64(3),
    last_activity_at DateTime64(3),
    ended_at Nullable(DateTime64(3)),
    created_at DateTime64(3) DEFAULT now64()
) ENGINE = MergeTree
PARTITION BY toYYYYMM(started_at)
ORDER BY (entity_id, started_at, session_id)
TTL toDateTime(started_at) + INTERVAL 180 DAY DELETE;
```

```sql
CREATE TABLE IF NOT EXISTS tracking.tracking_events
(
    id UUID,
    entity_id UUID,
    session_id UUID,
    event_type String,
    event_name String,
    page_name String,
    component_name String,
    timestamp DateTime64(3),
    refer String,
    expose_time Int32,
    employee_id UUID,
    company_id UUID,
    device_type String,
    os_version String,
    browser_version String,
    network_type String,
    network_effective_type String,
    page_url String,
    page_title String,
    viewport_height Int32,
    properties String
) ENGINE = MergeTree
PARTITION BY toYYYYMM(timestamp)
ORDER BY (entity_id, session_id, timestamp, id)
TTL toDateTime(timestamp) + INTERVAL 180 DAY DELETE;
```

```sql
CREATE TABLE IF NOT EXISTS tracking.dashboard_editors
(
    id UUID,
    entity_id UUID,
    user_email String,
    added_at DateTime64(3),
    added_by String
) ENGINE = ReplacingMergeTree
ORDER BY (entity_id, id);
```

```sql
CREATE TABLE IF NOT EXISTS tracking.dashboard_favorites
(
    id UUID,
    entity_id UUID,
    user_id UInt64,
    created_at DateTime64(3)
) ENGINE = ReplacingMergeTree
ORDER BY (entity_id, id);
```

## Cascade strategy
- ClickHouse has no FK cascades; the API issues ordered `ALTER ... DELETE`
  mutations for children then parent (`tracking_events`, `tracking_sessions`,
  `dashboard_editors`, `dashboard_favorites`, `main_entities`).
- Order keys include `entity_id` to keep related rows co-located for deletes.

## Retention
- `main_entities` TTL: 365 days from `created_at`.
- `tracking_sessions` TTL: 180 days from `started_at`.
- `tracking_events` TTL: 180 days from `timestamp`.

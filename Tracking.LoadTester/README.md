# Tracking.LoadTester

Lightweight console app that stress-tests the Tracking API against ClickHouse by creating a PT entity, one session, and firing a configurable number of events in parallel.

## Prerequisites
- .NET SDK 10.0+
- Tracking API running (e.g., `dotnet run --project Tracking.Api/Tracking.Api.csproj --urls http://localhost:8080`)
- ClickHouse reachable by the API (compose default creds: `clickhouse / Pa$$w0rd`)

## Build
```bash
dotnet build Tracking.LoadTester/Tracking.LoadTester.csproj
```

## Run
```bash
set TARGET_BASE=http://localhost:8080     # API base URL
set TOTAL_EVENTS=100000                   # how many events to send
set CONCURRENCY=64                        # parallel workers
# optionally reuse existing IDs:
# set ENTITY_ID=<guid>
# set SESSION_ID=<guid>
dotnet run --project Tracking.LoadTester/Tracking.LoadTester.csproj
```

Console output shows entity/session IDs, per-event logging, total time, failures, and avg RPS.

## Environment variables
- `TARGET_BASE` (default `http://localhost:8080`) – API base URL
- `TOTAL_EVENTS` (default `100000`) – number of events to send
- `CONCURRENCY` (default `64`) – parallel worker count
- `ENTITY_ID` (optional) – reuse an existing main entity
- `SESSION_ID` (optional) – reuse an existing session (must match `ENTITY_ID`)

## What it does
1. Creates an entity production “PT” unless `ENTITY_ID` is provided.
2. Creates a session tied to that entity unless `SESSION_ID` is provided.
3. Sends `TOTAL_EVENTS` POSTs to `POST /entities/{entityId}/events` with realistic payloads (behavior events, page metadata, device/network info).

## Tuning tips
- Increase `CONCURRENCY` to find throughput ceiling; expect higher p95/p99 latency and potential 5xx/timeout if you overshoot.
- Decrease `CONCURRENCY` for smoother latency at lower RPS.
- Watch API logs and ClickHouse metrics (CPU, merges, disk I/O) during runs.

## Checking storage size in ClickHouse
From the ClickHouse client:
```sql
SELECT
  table,
  formatReadableSize(sum(data_bytes)) AS on_disk,
  sum(rows) AS rows
FROM system.parts
WHERE database = 'tracking' AND active
GROUP BY table
ORDER BY sum(data_bytes) DESC;
```

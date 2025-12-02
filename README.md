# Tracking API & ClickHouse

API and ClickHouse schema for tracking dashboards, sessions, and events.

## Stack
- ASP.NET Core 9 minimal controllers (`Tracking.Api/`).
- ClickHouse backing store (`db/001_clickhouse_init.sql`).
- Docker Compose for API + ClickHouse (`docker-compose.yaml`, `docker-compose.override.yaml` for watch mode).

## Quick Start
```bash
docker-compose up --build
# API: http://localhost:8080
# Swagger: http://localhost:8080/swagger
# ClickHouse: localhost:8123 (HTTP), 9000 (native)
```
Default ClickHouse credentials (compose): `clickhouse` / `Pa$$w0rd` against database `tracking`.

## Local Development (without Docker)
```bash
dotnet restore
dotnet run --project Tracking.Api/Tracking.Api.csproj --urls http://localhost:8080
```
Start ClickHouse separately (e.g., from compose: `docker-compose up clickhouse`).

## Schema
- Canonical DDL: `db/001_clickhouse_init.sql`
- Reference: `DBSchema.md`

## Sample Requests
Create a session (auto-creates entities if missing):
```bash
curl -X POST http://localhost:8080/sessions \
  -H "Content-Type: application/json" \
  -H "Cookie: __ModuleSessionCookie=<jwt_with_cid_claim>" \
  -d '{
        "employeeId":"11111111-2222-3333-4444-555555555555",
        "companyId":"aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee",
        "startedAt":"2024-01-01T00:00:00Z",
        "entity":{
          "panels":"{}",
          "collaborators":"[]",
          "visibility":"private",
          "isShared":false
        }
      }'
```
Create an event (let the API create the session/entity if needed):
```bash
curl -X POST http://localhost:8080/entities/events \
  -H "Content-Type: application/json" \
  -d '{"production":"PT","eventType":"click","eventName":"cta"}'
```
Health check: `curl http://localhost:8080/health`

## Testing / Validation
- Build: `dotnet build Tracking.sln`
- Unit tests: `dotnet test Tracking.sln` (xUnit)
- Coverage: `dotnet test Tracking.sln --settings coverlet.runsettings --collect:"XPlat Code Coverage"` (Cobertura XML lands under `Tracking.Api.Tests/TestResults/.../coverage.cobertura.xml`)
- Coverage HTML (requires reportgenerator): `dotnet tool install -g dotnet-reportgenerator-globaltool` then `reportgenerator -reports:**/coverage.cobertura.xml -targetdir:coverage-report -reporttypes:HtmlSummary`
- Manual API checks via Swagger or the `Tracking.Api/Tracking.Api.http` file.
- ClickHouse validation:
```bash
docker-compose exec clickhouse clickhouse-client \
  --user clickhouse --password 'Pa$$w0rd' \
  --query "SELECT count() FROM tracking.tracking_events"
```

## Event Queue
- Event POSTs (`/entities/events` or `/entities/{sessionId}/events`) are queued in-memory and return `202 Accepted` on enqueue with the `sessionId` used. If the queue is full, the API returns `429 Too Many Requests`.
- If you omit `sessionId` (or send empty), the API generates one and echoes it in the response so you can reuse it.
- Queue status endpoint: `GET /entities/events/queue-status` returns `capacity`, `current_depth`, `enqueued`, `dropped`, `processed`, `failed`.
- Queue is bounded (default 10,000). Requests beyond capacity are dropped and counted as `dropped`.

## Load Testing
Use the console load tester in `Tracking.LoadTester` to generate entities, sessions, and a firehose of events.
```bash
dotnet run --project Tracking.LoadTester/Tracking.LoadTester.csproj
# optional env vars:
# TARGET_BASE=http://localhost:8080
# TOTAL_EVENTS=100000
# CONCURRENCY=64
# PROGRESS_STEP=1000
# ENTITY_ID=<guid> SESSION_ID=<guid>  # reuse existing
```
It creates a PT entity + session (unless you provide IDs) and sends `TOTAL_EVENTS` POSTs to `/entities/{entityId}/events`, printing in-place progress and RPS on completion.

Dockerized run (isolated profile so it only runs when you ask):
```bash
docker-compose --profile loadtest run --rm loadtester
# override envs with -e/--env-file if needed
```

## Notes
- Cascades are handled in the API via ordered `ALTER ... DELETE` mutations on child tables before deleting `main_entities`.
- Override connection strings with env var `ClickHouse__ConnectionString`. Keep secrets out of git.***

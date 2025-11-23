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
Create an entity:
```bash
curl -X POST http://localhost:8080/entities \
  -H "Content-Type: application/json" \
  -d '{"creatorId":1,"creatorEmail":"owner@example.com","title":"demo"}'
```
Create a session:
```bash
curl -X POST http://localhost:8080/entities/{entityId}/sessions \
  -H "Content-Type: application/json" \
  -d '{"userId":1,"companyId":1,"deviceType":"web"}'
```
Create an event:
```bash
curl -X POST http://localhost:8080/entities/{entityId}/events \
  -H "Content-Type: application/json" \
  -d '{"sessionId":"<session-id>","eventType":"click","eventName":"cta"}'
```
Health check: `curl http://localhost:8080/health`

## Testing / Validation
- Build: `dotnet build Tracking.sln`
- Manual API checks via Swagger or the `Tracking.Api/Tracking.Api.http` file.
- ClickHouse validation:
```bash
docker-compose exec clickhouse clickhouse-client \
  --user clickhouse --password 'Pa$$w0rd' \
  --query "SELECT count() FROM tracking.tracking_events"
```

## Notes
- Cascades are handled in the API via ordered `ALTER ... DELETE` mutations on child tables before deleting `main_entities`.
- Override connection strings with env var `ClickHouse__ConnectionString`. Keep secrets out of git.***

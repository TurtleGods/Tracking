# Repository Guidelines

## Project Structure & Module Organization
- `Tracking.Api/`: ASP.NET Core 9 backend, `Program.cs` plus controllers under `Controllers/`, data access in `Data/`, DTOs in `Models/` and `Requests/`.
- `db/`: ClickHouse DDL and user configs. Schema lives in `db/001_clickhouse_init.sql`; user overrides in `db/users.d/`.
- `docker-compose.yaml` (+ `docker-compose.override.yaml` for dev hot-reload): brings up API and ClickHouse.
- `DBSchema.md`: canonical schema reference; keep in sync with `db/001_clickhouse_init.sql`.
- Sample REST calls in `Tracking.Api/Tracking.Api.http`.

## Build, Test, and Development Commands
- Restore/build: `dotnet build Tracking.sln`.
- Local run: `dotnet run --project Tracking.Api/Tracking.Api.csproj --urls http://localhost:8080`.
- Docker stack: `docker-compose up --build` (uses override for `dotnet watch` if present).
- ClickHouse client (inside container): `docker-compose exec clickhouse clickhouse-client --user clickhouse --password 'Pa$$w0rd'`.
- Swagger UI: visit `http://localhost:8080/swagger` when API is running.

## Coding Style & Naming Conventions
- C#: nullable enabled, implicit usings; keep types and DTOs in PascalCase, JSON fields lower_snake_case via `[JsonPropertyName]`.
- Tables: plural lower_snake_case; columns lower_snake_case (`UUID`, `UInt64`, `DateTime64`, `jsonb`-style strings).
- Prefer parameterized queries (ClickHouse.Client) over string interpolation; keep repository methods minimal and composable.
- SQL formatting: align columns and clauses; keep lines near ~100 chars.

## Testing Guidelines
- No automated tests yet; rely on `dotnet build` and manual endpoint checks via Swagger or `*.http` file.
- For ClickHouse, validate DDL via `clickhouse-client` and run sample `SELECT COUNT(*)` on loaded fixtures.
- When adding tests, mirror migration names if you introduce pgTAP/other frameworks; keep fixtures in `db` or `seeds/`.

## Commit & Pull Request Guidelines
- Commit messages: short imperative subject (≤72 chars), optional body for schema rationale/migration impact.
- PRs should describe schema/API changes, expected impacts (e.g., TTLs, nullable columns), and migration order if relevant.
- Include evidence of validation (build output, clickhouse-client checks, screenshots of Swagger if UI changes).

## Security & Configuration Tips
- Do not commit secrets; `.env` is ignored. Use env vars for `ClickHouse__ConnectionString`.
- Default ClickHouse creds in compose: `clickhouse / Pa$$w0rd`; adjust via `db/users.d` and compose env vars.
- Keep generated binaries/build artifacts out of version control (`obj/`, `bin/` already in `.gitignore`).***

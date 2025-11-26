# Project Structure

High-level layout (build artifacts `bin/` and `obj/` omitted):

```
.
├─ Tracking.sln
├─ Tracking.Api/
│  ├─ Program.cs
│  ├─ Controllers/            # Entities, sessions, events, and health endpoints
│  ├─ Data/                   # ClickHouse options, connection factory, repository
│  ├─ Models/                 # DTOs returned by the API
│  ├─ Requests/               # Request payload contracts
│  ├─ Properties/             # launchSettings for local debugging
│  ├─ appsettings*.json       # API configuration (base + Development)
│  ├─ Dockerfile              # API image build
│  └─ Tracking.Api.http       # Sample REST calls
├─ Tracking.LoadTester/
│  ├─ Program.cs              # Console load generator
│  ├─ Tracking.LoadTester.csproj
│  └─ README.md               # Usage and env vars
├─ db/
│  ├─ 001_clickhouse_init.sql # Canonical ClickHouse schema
│  └─ users.d/                # ClickHouse user overrides
├─ docker-compose.yaml        # API + ClickHouse stack
├─ DBSchema.md                # Schema reference (mirrors DDL)
├─ README.md                  # Project overview and quick start
└─ .vscode/launch.json        # Debug profiles
```

Notes:
- API is ASP.NET Core 9 with minimal controllers; Swagger available at `/swagger` when running.
- ClickHouse connection string is provided via `ClickHouse__ConnectionString` env var; defaults live in compose.
- Load tester targets the running API to seed entities/sessions and generate events at scale.

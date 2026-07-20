# TimeLens

TimeLens is an open-source Time Series Intelligence Platform for ingesting, validating, processing, and analyzing time series data from multiple providers. It combines .NET services, PostgreSQL scheduling state, ClickHouse time-series persistence, RabbitMQ-based workers, and a React/Vite UI for dataset exploration, ingestion monitoring, validation, and execution workflows.

## What It Does

- Search provider-backed curves and datasets by endpoint, metric, curve id, or identifier.
- Inspect current or historical time-series versions in charts and tables.
- Track each curve's ingestion schedules, queued jobs, executions, inserted rows, skipped rows, and failures.
- Edit ingestion schedules, reset their default cron/window, and queue one-time backload jobs.
- Validate data quality and record validation findings.
- Run execution workflows over stored time-series data.
- Run the full stack locally with Compose.

## Project Status

TimeLens currently covers data ingestion, versioned storage, an execution platform, and a validation engine. Planned work is tracked in [ROADMAP.md](ROADMAP.md), including analytics, derived curves, multi-provider support, signals, forecasting, strategy execution, and backtesting.

## Stack

- Backend: .NET 10
- UI: React 19 + Vite
- Data: PostgreSQL 17 + ClickHouse 25.3
- Messaging: RabbitMQ 4 Management
- Tests: xUnit and Vitest
- Containers: Dockerfiles plus `docker-compose.yml`, usable with Podman Compose or Docker Compose

## Services

| Service | Purpose | Port |
| --- | --- | --- |
| `ui` | React/Vite app served by Nginx | `8080` |
| `api` | Main HTTP API, dataset reads, historical time-series reads, and ingestion control | `5100` |
| `insert` | Insert API plus gRPC ingestion endpoint | `5101`, `5103` |
| `scheduler` | Queues ingestion jobs on schedule | internal |
| `ingestion` | Consumes RabbitMQ jobs and loads provider data | internal |
| `postgres` | Scheduling, jobs, executions, and assets | `5432` |
| `clickhouse` | Ingested time-series data | `8123`, `9000` |
| `rabbitmq` | RabbitMQ broker and management UI | `5672`, `15672` |

## Time-Series Reads

Series endpoints accept exact datetimes or simple expressions for `start`, `end`, and `asOf`:

```text
GET /api/datasets/{datasetId}/series?start=today-1&end=now&asOf=today&timeZone=UTC
GET /api/curves/{meterPointId}/series?start=2026-07-18T00:00&end=2026-07-19T00:00&asOf=now&timeZone=Europe/Copenhagen
```

Supported expressions are `now`, `today`, `today+N`, `today-N`, `now+Nh`, and `now-Nh`. `today` and exact datetimes without an offset use the optional `timeZone` query value; omit it for UTC. `asOf` means version time: the API returns the newest value inserted at or before that time. Omit `asOf` for the latest version.

## Curve-Scoped Ingestion

Schedules, jobs, and job executions are organized by scheduler `CurveId`, for example `dk.public_power` or `DK1.price`. During ingestion, that `CurveId` is stored on dataset metadata so the UI can start from a curve and then show only that curve's datasets, schedules, jobs, and executions.

Useful query endpoints:

```text
GET /api/datasets?curveId={curveId}
GET /api/ingestion/curves/{curveId}/schedules
GET /api/ingestion/curves/{curveId}/jobs
GET /api/ingestion/curves/{curveId}/executions
PUT /api/ingestion/schedules/{scheduleId}
POST /api/ingestion/schedules/{scheduleId}/reset
POST /api/ingestion/schedules/{scheduleId}/backloads
```

Older global ingestion endpoints still exist for compatibility, but the UI uses the curve-scoped endpoints.

Schedules and jobs carry a `Source`. The current implemented provider is `energy-charts`; adding FTP, Selenium, or another source should add a source-specific ingestion implementation and route by that field, while keeping metadata in PostgreSQL and time-series values in ClickHouse.

## Run With Compose

Podman:

```bash
podman compose build
podman compose up -d
```

Docker:

```bash
docker compose build
docker compose up -d
```

Then open:

- UI: `http://localhost:8080`
- RabbitMQ management: `http://localhost:15672`

Stop the stack:

```bash
podman compose down
```

Use `docker compose down` if you started it with Docker.

## Local Development

Backend:

```bash
dotnet restore TimeLens.sln
dotnet build TimeLens.sln
dotnet run --project src/TimeLens.API/TimeLens.API.csproj
```

Client:

```bash
cd src/TimeLens.Client
npm install
npm start
```

The client expects Node `>=24.18.0 <27` and npm `>=12 <13`. Set `VITE_API_BASE_URL` if the API is not available through `/api`.

## Tests

```bash
dotnet test TimeLens.sln
cd src/TimeLens.Client && npm test
```

## License

TimeLens is licensed under the [MIT License](LICENSE).

## Notes

- Compose uses database name `timelens` for PostgreSQL and ClickHouse.
- Copy `.env.example` to `.env` and set the passwords before running compose.
- The UI container installs npm 12 in its Node 24 build image before running `npm ci`.
- Existing dataset metadata created before curve scoping may need re-ingestion/upsert to populate `CurveId`.
- Current .NET builds may report NuGet audit warnings for transitive package vulnerabilities; treat those separately from compile errors.

# EcoAssetHub

EcoAssetHub is a renewable-energy data platform for collecting, storing, querying, and visualizing Energy Charts time-series data. It combines .NET services, PostgreSQL scheduling state, ClickHouse time-series persistence, RabbitMQ-based ingestion workers, and a React/Vite UI for dataset exploration and ingestion monitoring.

## What It Does

- Search Energy Charts curves and datasets by endpoint, metric, curve id, or identifier.
- Inspect production and forecast time-series data in charts and tables.
- Track each curve's ingestion schedules, queued jobs, executions, inserted rows, skipped rows, and failures.
- Run the full stack locally with Compose.

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
| `api` | Main HTTP API, dataset reads, and time-series reads | `5100` |
| `insert` | Insert API plus gRPC ingestion endpoint | `5101`, `5103` |
| `scheduler` | Queues ingestion jobs on schedule | internal |
| `ingestion` | Consumes RabbitMQ jobs and loads Energy Charts data | internal |
| `postgres` | Scheduling, jobs, executions, and assets | `5432` |
| `clickhouse` | Ingested dataset metadata and time-series data | `8123`, `9000` |
| `rabbitmq` | RabbitMQ broker and management UI | `5672`, `15672` |

## Curve-Scoped Ingestion

Schedules, jobs, and job executions are organized by scheduler `CurveId`, for example `dk.public_power` or `DK1.price`. During ingestion, that `CurveId` is stored on dataset metadata so the UI can start from a curve and then show only that curve's datasets, schedules, jobs, and executions.

Useful query endpoints:

```text
GET /api/datasets?curveId={curveId}
GET /api/ingestion/curves/{curveId}/schedules
GET /api/ingestion/curves/{curveId}/jobs
GET /api/ingestion/curves/{curveId}/executions
```

Older global ingestion endpoints still exist for compatibility, but the UI uses the curve-scoped endpoints.

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
dotnet restore EcoAssetHub.sln
dotnet build EcoAssetHub.sln
dotnet run --project src/EcoAssetHub.API/EcoAssetHub.API.csproj
```

Client:

```bash
cd src/EcoAssetHub.Client
npm install
npm start
```

The client expects Node `>=24.18.0 <27` and npm `>=12 <13`. Set `VITE_API_BASE_URL` if the API is not available through `/api`.

## Tests

```bash
dotnet test EcoAssetHub.sln
cd src/EcoAssetHub.Client && npm test
```

## Notes

- Compose uses database name `ecoassethub` for PostgreSQL and ClickHouse.
- The UI container installs npm 12 in its Node 24 build image before running `npm ci`.
- Existing dataset metadata created before curve scoping may need re-ingestion/upsert to populate `CurveId`.
- Current .NET builds may report NuGet audit warnings for transitive package vulnerabilities; treat those separately from compile errors.

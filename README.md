# EcoAssetHub

EcoAssetHub is a renewable-energy data platform for collecting, storing, querying, and visualizing Energy Charts time-series data. It combines .NET services, MongoDB persistence, RabbitMQ-based ingestion workers, and a React/Vite UI for dataset exploration and ingestion monitoring.

## What It Does

- Search Energy Charts datasets by endpoint, metric, or identifier.
- Inspect production and forecast time-series data in charts and tables.
- Track ingestion schedules, queued jobs, executions, inserted rows, skipped rows, and failures.
- Run the full stack locally with Compose.

## Stack

- Backend: .NET 10
- UI: React 19 + Vite
- Data: MongoDB 7
- Messaging: RabbitMQ 3 Management
- Tests: xUnit and Vitest
- Containers: Dockerfiles plus `docker-compose.yml`, usable with Podman Compose or Docker Compose

## Services

| Service | Purpose | Port |
| --- | --- | --- |
| `ui` | React/Vite app served by Nginx | `8080` |
| `api` | Main HTTP API | `5100` |
| `insert` | Insert API plus gRPC ingestion endpoint | `5101`, `5103` |
| `query` | Dataset and time-series query API | `5102` |
| `scheduler` | Queues ingestion jobs on schedule | internal |
| `ingestion` | Consumes RabbitMQ jobs and loads Energy Charts data | internal |
| `mongo` | MongoDB database | `27017` |
| `rabbitmq` | RabbitMQ broker and management UI | `5672`, `15672` |

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
dotnet run --project src/EcoAssetHub.Query/EcoAssetHub.Query.csproj
```

Client:

```bash
cd src/EcoAssetHub.Client
npm install
npm start
```

The client expects Node `>=24.18.0 <27` and npm `>=12 <13`. Set `VITE_API_BASE_URL` if the query API is not available through `/api`.

## Tests

```bash
dotnet test EcoAssetHub.sln
cd src/EcoAssetHub.Client && npm test
```

## Notes

- Compose uses database name `centrica` and MongoDB connection string `mongodb://mongo:27017`.
- The UI container installs npm 12 in its Node 24 build image before running `npm ci`.
- Current .NET builds may report NuGet audit warnings for transitive package vulnerabilities; treat those separately from compile errors.

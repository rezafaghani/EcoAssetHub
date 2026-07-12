# Repository Guidelines

## Project Structure & Module Organization

EcoAssetHub is a .NET 10 solution with a React/Vite client. Backend projects live under `src/`: `EcoAssetHub.API` is the main HTTP API, `EcoAssetHub.Query` serves dataset/time-series reads, `EcoAssetHub.Insert` handles inserts and gRPC ingestion writes, and `EcoAssetHub.Ingestion` plus `EcoAssetHub.Scheduler` run background ingestion. Shared domain models and interfaces are in `src/EcoAssetHub.Domain`; MongoDB repositories and infrastructure live in `src/EcoAssetHub.Infrastructure`. The client app is in `src/EcoAssetHub.Client`. Unit tests are in `tests/EcoAssetHub.UnitTest`, with fixtures in `tests/EcoAssetHub.UnitTest/TestData`.

## Build, Test, and Development Commands

- `dotnet restore EcoAssetHub.sln`: restore backend dependencies.
- `dotnet build EcoAssetHub.sln`: compile all .NET projects.
- `dotnet test EcoAssetHub.sln`: run xUnit backend tests.
- `dotnet run --project src/EcoAssetHub.Query/EcoAssetHub.Query.csproj`: run the query API locally.
- `cd src/EcoAssetHub.Client && npm install`: install client dependencies.
- `cd src/EcoAssetHub.Client && npm start`: start Vite on `127.0.0.1`.
- `cd src/EcoAssetHub.Client && npm test`: run Vitest.
- `docker compose up -d` or `podman compose up -d`: run the full stack from `docker-compose.yml`.

## Coding Style & Naming Conventions

C# projects use nullable reference types and implicit usings. Keep namespaces, folders, and types aligned with existing patterns such as `Repositories/*Repository.cs`, `Controllers/*Controller.cs`, and command/query handler folders. Use PascalCase for C# types and methods, camelCase for locals and parameters. The client `.editorconfig` requires UTF-8, spaces, 2-space indentation, final newlines, and single quotes in TypeScript.

## Testing Guidelines

Backend tests use xUnit, Moq, and coverlet. Name files after the class or behavior under test, for example `ProductionRepositoryTests.cs` or `CreateWindTurbineCommandHandlerTests.cs`. Keep test fixtures in `TestData` and mark them for output copying when needed. Client tests use Vitest with `*.test.ts` or `*.test.tsx` files.

## Commit & Pull Request Guidelines

Recent history uses Conventional Commit-style subjects such as `feat: Implement ingestion service`, `fix: compact forecast dataset panel`, and `chore: clean up React client`. Keep commits focused and imperative. Pull requests should describe the change, list test commands run, link issues when applicable, and include screenshots for visible UI changes.

## Security & Configuration Tips

Keep secrets out of `appsettings.json` and client source. Use environment variables for connection strings, API URLs, and credentials. Compose defaults include MongoDB at `mongodb://mongo:27017` and RabbitMQ management on `localhost:15672`; avoid committing local overrides.

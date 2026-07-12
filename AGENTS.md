# Repository Guidelines

## Project Structure & Module Organization
EcoAssetHub is organized as a .NET solution with an Angular client:

- `src/EcoAssetHub.API`: ASP.NET Core API, controllers, MediatR commands/queries, workers, app settings, and CSV seed data in `FileData/`.
- `src/EcoAssetHub.Domain`: entities, domain models, repository interfaces, and domain exceptions.
- `src/EcoAssetHub.Infrastructure`: persistence context and repository implementations.
- `src/EcoAssetHub.Client`: Angular application, components, services, routes, styles, and frontend tests.
- `tests/EcoAssetHub.UnitTest`: xUnit tests plus test CSV data in `TestData/`.

Keep backend features aligned with the CQRS-style folders under `Application/*Commands`. Put shared contracts in `Domain`, persistence details in `Infrastructure`, and HTTP entry points in `API/Controllers`.

## Build, Test, and Development Commands

- `dotnet restore EcoAssetHub.sln`: restore backend and test project packages.
- `dotnet build EcoAssetHub.sln`: compile the full .NET solution.
- `dotnet test EcoAssetHub.sln`: run xUnit tests in `tests/EcoAssetHub.UnitTest`.
- `dotnet run --project src/EcoAssetHub.API/EcoAssetHub.API.csproj`: run the API locally.
- `cd src/EcoAssetHub.Client && npm install`: install Angular dependencies.
- `cd src/EcoAssetHub.Client && npm start`: run Angular dev server on `127.0.0.1`.
- `cd src/EcoAssetHub.Client && npm run build`: build the Angular app.
- `cd src/EcoAssetHub.Client && npm test`: run Jasmine/Karma frontend tests.

## Coding Style & Naming Conventions

C# projects use nullable reference types and implicit usings. Use PascalCase for types and methods, camelCase for locals and parameters, and `I*Repository` for repository interfaces. Name command/query files by action, for example `CreateWindTurbineCommandHandler.cs`.

The Angular `.editorconfig` uses UTF-8, two-space indentation, final newlines, and single quotes for TypeScript. Keep Angular files in the standard `*.component.ts/html/css/spec.ts` pattern.

## Testing Guidelines

Backend tests use xUnit, Moq, and coverlet. Place tests near the matching feature area under `tests/EcoAssetHub.UnitTest`, and name classes after the unit under test, such as `CreateSolarPanelCommandHandlerTests`. Frontend specs live beside components and services as `*.spec.ts`.

Run relevant tests before submitting changes; use full `dotnet test EcoAssetHub.sln` for backend changes that touch shared behavior.

## Commit & Pull Request Guidelines

Recent history uses short messages, often Conventional Commit prefixes such as `fix:` and `feat:`. Prefer concise, imperative messages like `fix: validate solar panel capacity`.

Pull requests should include a brief summary, test results, linked issues when available, and screenshots for UI changes. Call out configuration, data, or migration impacts explicitly.

## Security & Configuration Tips

Do not commit secrets. Keep local connection strings and credentials in user secrets or environment-specific settings. Treat CSV files in `FileData/` and `TestData/` as fixtures; avoid replacing them without updating affected tests.

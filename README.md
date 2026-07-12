# EcoAssetHub

## Overview
EcoAssetHub is a renewable-energy data platform built around a .NET backend and a React/Vite client. The current UI focuses on dataset exploration, Energy Charts time-series inspection, and ingestion status monitoring.

## Features
- **Dataset explorer:** Search datasets by endpoint, metric, or identifier.
- **Time-series viewer:** Inspect charted production and forecast data.
- **Ingestion monitoring:** Review schedules, jobs, and executions.

## Technologies Used
- Backend: .NET 8
- Frontend: React 19 + Vite
- Data access: MongoDB
- Testing: xUnit, Vitest

## Setup and Installation
1. Install the .NET 8 SDK.
2. Install Node.js 20+.
3. Install MongoDB if you want to run the API against a local instance.

## Usage
Backend:
```bash
dotnet restore EcoAssetHub.sln
dotnet run --project src/EcoAssetHub.API/EcoAssetHub.API.csproj
```

Client:
```bash
cd src/EcoAssetHub.Client
npm install
npm start
```

## Development and Contribution
Run the relevant build or test command before submitting changes:

- `dotnet build EcoAssetHub.sln`
- `dotnet test EcoAssetHub.sln`
- `cd src/EcoAssetHub.Client && npm run build`
- `cd src/EcoAssetHub.Client && npm test`

## License
This project is licensed under the MIT License.

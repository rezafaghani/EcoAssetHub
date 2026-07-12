FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EcoAssetHub.Scheduler/EcoAssetHub.Scheduler.csproj", "src/EcoAssetHub.Scheduler/"]
COPY ["src/EcoAssetHub.Domain/EcoAssetHub.Domain.csproj", "src/EcoAssetHub.Domain/"]
COPY ["src/EcoAssetHub.Infrastructure/EcoAssetHub.Infrastructure.csproj", "src/EcoAssetHub.Infrastructure/"]
RUN dotnet restore "src/EcoAssetHub.Scheduler/EcoAssetHub.Scheduler.csproj"
COPY . .
RUN dotnet publish "src/EcoAssetHub.Scheduler/EcoAssetHub.Scheduler.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcoAssetHub.Scheduler.dll"]

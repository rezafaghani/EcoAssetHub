FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EcoAssetHub.Ingestion/EcoAssetHub.Ingestion.csproj", "src/EcoAssetHub.Ingestion/"]
COPY ["src/EcoAssetHub.Domain/EcoAssetHub.Domain.csproj", "src/EcoAssetHub.Domain/"]
RUN dotnet restore "src/EcoAssetHub.Ingestion/EcoAssetHub.Ingestion.csproj"
COPY . .
RUN dotnet publish "src/EcoAssetHub.Ingestion/EcoAssetHub.Ingestion.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/runtime:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "EcoAssetHub.Ingestion.dll"]

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["src/EcoAssetHub.Insert/EcoAssetHub.Insert.csproj", "src/EcoAssetHub.Insert/"]
COPY ["src/EcoAssetHub.Domain/EcoAssetHub.Domain.csproj", "src/EcoAssetHub.Domain/"]
COPY ["src/EcoAssetHub.Infrastructure/EcoAssetHub.Infrastructure.csproj", "src/EcoAssetHub.Infrastructure/"]
RUN dotnet restore "src/EcoAssetHub.Insert/EcoAssetHub.Insert.csproj"
COPY . .
RUN dotnet publish "src/EcoAssetHub.Insert/EcoAssetHub.Insert.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime
WORKDIR /app
COPY --from=build /app/publish .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "EcoAssetHub.Insert.dll"]

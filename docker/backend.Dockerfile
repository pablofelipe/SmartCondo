FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/src/SmartCondoApi/SmartCondoApi.csproj SmartCondoApi/
RUN dotnet restore SmartCondoApi/SmartCondoApi.csproj

COPY backend/src/SmartCondoApi/ SmartCondoApi/
RUN dotnet publish SmartCondoApi/SmartCondoApi.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app

# curl is only here so docker-compose's healthcheck can call /health/live.
RUN apt-get update && apt-get install -y --no-install-recommends curl \
    && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "SmartCondoApi.dll"]

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/PollingSystem.Data/PollingSystem.Data.csproj backend/PollingSystem.Data/
COPY backend/PollingSystem.Messaging/PollingSystem.Messaging.csproj backend/PollingSystem.Messaging/
COPY backend/PollingSystem.Api/PollingSystem.Api.csproj backend/PollingSystem.Api/
COPY backend/PollingSystem.Worker/PollingSystem.Worker.csproj backend/PollingSystem.Worker/

RUN dotnet restore backend/PollingSystem.Api/PollingSystem.Api.csproj
RUN dotnet restore backend/PollingSystem.Worker/PollingSystem.Worker.csproj

COPY backend/ backend/
RUN dotnet publish backend/PollingSystem.Api/PollingSystem.Api.csproj \
    --configuration Release \
    --output /app/api \
    --no-restore
RUN dotnet publish backend/PollingSystem.Worker/PollingSystem.Worker.csproj \
    --configuration Release \
    --output /app/worker \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS api
WORKDIR /app
COPY --from=build /app/api .
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "PollingSystem.Api.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS worker
WORKDIR /app
COPY --from=build /app/worker .
ENTRYPOINT ["dotnet", "PollingSystem.Worker.dll"]

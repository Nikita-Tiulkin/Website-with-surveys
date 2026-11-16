FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY backend/PollingSystem.Data/PollingSystem.Data.csproj backend/PollingSystem.Data/
COPY backend/PollingSystem.Api/PollingSystem.Api.csproj backend/PollingSystem.Api/
RUN dotnet restore backend/PollingSystem.Api/PollingSystem.Api.csproj

COPY backend/ backend/
RUN dotnet publish backend/PollingSystem.Api/PollingSystem.Api.csproj \
    --configuration Release \
    --output /app/publish \
    --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0
WORKDIR /app
COPY --from=build /app/publish .

ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080

ENTRYPOINT ["dotnet", "PollingSystem.Api.dll"]

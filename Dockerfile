# Restore
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS restore-lib
WORKDIR /src
COPY AntennaScraper.sln .
COPY src/AntennaScraper.Lib/AntennaScraper.Lib.csproj src/AntennaScraper.Lib/
COPY src/AntennaScraper.Api/AntennaScraper.Api.csproj src/AntennaScraper.Api/
COPY src/AntennaScraper.Migrator/AntennaScraper.Migrator.csproj src/AntennaScraper.Migrator/
COPY src/AntennaScraper.Tests.Lib/AntennaScraper.Tests.Lib.csproj src/AntennaScraper.Tests.Lib/
RUN dotnet restore

# Build lib
FROM restore-lib AS build-lib
WORKDIR /src
COPY src/AntennaScraper.Lib src/AntennaScraper.Lib/
RUN dotnet build src/AntennaScraper.Lib/AntennaScraper.Lib.csproj -c Release --no-restore

# Publish API
FROM build-lib AS publish-api
WORKDIR /src
COPY src/AntennaScraper.Api src/AntennaScraper.Api/
RUN dotnet publish src/AntennaScraper.Api/AntennaScraper.Api.csproj -c Release -o /app/publish -p:UseAppHost=false

# Publish migrator
FROM build-lib AS publish-migrator
WORKDIR /src
COPY src/AntennaScraper.Migrator src/AntennaScraper.Migrator/
RUN dotnet publish src/AntennaScraper.Migrator/AntennaScraper.Migrator.csproj -c Release -o /app/publish -p:UseAppHost=false

# Runtime API
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-api
WORKDIR /app
COPY --from=publish-api /app/publish .
EXPOSE 8080
ENTRYPOINT ["dotnet", "AntennaScraper.Api.dll"]

# Runtime migrator
FROM mcr.microsoft.com/dotnet/aspnet:10.0 AS runtime-migrator
WORKDIR /app
COPY --from=publish-migrator /app/publish .
ENTRYPOINT ["dotnet", "AntennaScraper.Migrator.dll"]
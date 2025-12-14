FROM mcr.microsoft.com/dotnet/aspnet:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY --from=server-core / "ServerAPIApp.Core/"
COPY --from=server-contracts / "ServerAPIApp.Contracts/"
COPY --from=server-domain / "ServerAPIApp.Domain/"
COPY --from=server-dal / "ServerAPIApp.DAL/"
COPY --from=shared / "Shared/"
COPY . "DbSeeder/"

WORKDIR /src/DbSeeder/

RUN dotnet restore -r linux-x64

RUN dotnet publish -c Release -o /app/publish \
    -p:PublishReadyToRun=true \
    -p:SelfContained=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .
COPY --from=appsettings /appsettings.json "appsettings.json"
COPY --from=data-seed / "SampleDbSeedingData/"

USER 1001

ENTRYPOINT ["dotnet", "DbSeeder.dll"]
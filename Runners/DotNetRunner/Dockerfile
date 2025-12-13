FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . "Runners/DotNetRunner/"
COPY --from=runners-shared / "Runners/Runners.Shared/"
COPY --from=shared / "Shared/"
COPY --from=server-domain / "ServerAPIApp.Domain/"

WORKDIR /src/Runners/DotNetRunner

RUN dotnet restore -r linux-x64

RUN dotnet publish -c Release -o /app/publish \
    -p:PublishReadyToRun=true \
    -p:SelfContained=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "DotNetRunner.dll"]
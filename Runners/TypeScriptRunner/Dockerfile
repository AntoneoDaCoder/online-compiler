FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . "Runners/TypeScriptRunner/"
COPY --from=runners-shared / "Runners/Runners.Shared/"
COPY --from=server-domain / "ServerAPIApp.Domain/"
COPY --from=shared / "Shared/"
COPY --from=supervisor / "RunnerSupervisor/"

WORKDIR /src/Runners/TypeScriptRunner

RUN dotnet restore -r linux-musl-x64

RUN dotnet publish -c Release -o /app/publish/TypeScript/ \
    -p:PublishReadyToRun=true \
    -r linux-musl-x64

WORKDIR /src/RunnerSupervisor

RUN dotnet restore -r linux-musl-x64

RUN dotnet publish -c Release -o /app/publish/Supervisor/ \
    -p:PublishReadyToRun=true \
    -r linux-musl-x64

FROM base AS final

RUN apk add --no-cache nodejs npm
RUN npm install -g typescript

WORKDIR /app
COPY --from=build /app/publish/TypeScript .
COPY --from=build /app/publish/Supervisor .

USER 1001

ENTRYPOINT ["./TypeScriptRunner"]

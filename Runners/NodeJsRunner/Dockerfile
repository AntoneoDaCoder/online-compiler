FROM mcr.microsoft.com/dotnet/runtime:10.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . "Runners/NodeJsRunner/"
COPY --from=runners-shared / "Runners/Runners.Shared/"
COPY --from=shared / "Shared/"
COPY --from=server-domain / "ServerAPIApp.Domain/"

RUN dotnet restore "Runners/NodeJsRunner/NodeJsRunner.csproj"

WORKDIR /src/Runners/NodeJsRunner
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final

RUN apk add --no-cache nodejs npm

WORKDIR /app
COPY --from=publish /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "NodeJsRunner.dll"]

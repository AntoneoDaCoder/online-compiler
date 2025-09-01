FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . "Runners/TypeScriptRunner/"
COPY --from=runners-shared / "Runners/Runners.Shared/"
COPY --from=shared / "Shared/"

RUN dotnet restore "Runners/TypeScriptRunner/TypeScriptRunner.csproj"

WORKDIR /src/Runners/TypeScriptRunner
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final

RUN apk add --no-cache nodejs npm
RUN npm install -g typescript

WORKDIR /app
COPY --from=publish /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "TypeScriptRunner.dll"]

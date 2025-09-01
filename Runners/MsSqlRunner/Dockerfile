FROM mcr.microsoft.com/dotnet/runtime:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . "Runners/MsSqlRunner/"
COPY --from=runners-shared / "Runners/Runners.Shared/"
COPY --from=shared / "Shared/"

RUN dotnet restore "Runners/MsSqlRunner/MsSqlRunner.csproj"

WORKDIR /src/Runners/MsSqlRunner
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app

RUN apk add --no-cache icu-libs icu-data-full
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false \
    LC_ALL=en_US.UTF-8 \
    LANG=en_US.UTF-8

COPY --from=publish /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "MsSqlRunner.dll"]
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY ["InMemoryRunner/","InMemoryRunner/"]

RUN dotnet restore "InMemoryRunner/InMemoryRunner.csproj"

WORKDIR /src/InMemoryRunner
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "InMemoryRunner.dll"]
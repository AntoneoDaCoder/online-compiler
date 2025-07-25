FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY ["Runners/DotNetRunner/","DotNetRunner/"]
COPY ["Shared/","Shared/"]

RUN dotnet restore "DotNetRunner/DotNetRunner.csproj"

WORKDIR /src/DotNetRunner
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "DotNetRunner.dll"]
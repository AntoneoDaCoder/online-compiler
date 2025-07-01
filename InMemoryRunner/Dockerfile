FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

RUN useradd -u 1001 -m runneruser

COPY . .

RUN dotnet restore "InMemoryRunner.csproj"
RUN dotnet publish -c Release -o /app/publish

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app

COPY --from=build /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "/app/InMemoryRunner.dll"]

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY --from=server-core / "ServerAPIApp.Core/"
COPY . "ServerAPIApp/"
COPY --from=shared / "Shared/"

RUN dotnet restore "ServerAPIApp/ServerAPIApp.csproj"

WORKDIR /src/ServerAPIApp
RUN dotnet build -c Release -o /app/build

FROM build AS publish
RUN dotnet publish -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .

ENTRYPOINT ["dotnet", "ServerAPIApp.dll"]
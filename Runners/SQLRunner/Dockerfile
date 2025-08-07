FROM mcr.microsoft.com/dotnet/runtime:9.0 AS base
USER $APP_UID
WORKDIR /app


FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
ARG BUILD_CONFIGURATION=Release
WORKDIR /src
COPY ["Runners/SqlRunner/SqlRunner.csproj", "Runners/SqlRunner/"]
RUN dotnet restore "./Runners/SqlRunner/SqlRunner.csproj"
COPY . .
WORKDIR "/src/Runners/SqlRunner"
RUN dotnet build "./SqlRunner.csproj" -c $BUILD_CONFIGURATION -o /app/build

FROM build AS publish
ARG BUILD_CONFIGURATION=Release
RUN dotnet publish "./SqlRunner.csproj" -c $BUILD_CONFIGURATION -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SqlRunner.dll"]
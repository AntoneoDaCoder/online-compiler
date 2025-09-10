ARG USE_COMPOSITE=false

FROM mcr.microsoft.com/dotnet/aspnet:9.0-alpine AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY --from=server-core / "ServerAPIApp.Core/"
COPY . "ServerAPIApp/"
COPY --from=shared / "Shared/"


RUN if [ "$USE_COMPOSITE" = "true" ]; then \
      sed -i 's/"UseComposite": false/"UseComposite": true/' ServerAPIApp/appsettings.json ; \
    fi

WORKDIR /src/ServerAPIApp/

RUN dotnet restore -r linux-x64

RUN dotnet publish -c Release -o /app/publish \
    -p:PublishReadyToRun=true \
    -p:SelfContained=false

FROM base AS final
WORKDIR /app
COPY --from=build /app/publish .

USER 1001

ENTRYPOINT ["dotnet", "ServerAPIApp.dll"]
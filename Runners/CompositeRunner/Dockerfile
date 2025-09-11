# ==========================================
# .NET builds
# ==========================================
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build-dotnet

WORKDIR /src

# Shared для всех раннеров
COPY ./Runners.Shared/ ./Runners/Runners.Shared/
COPY --from=shared / ./Shared/

# ---- CompositeRunner ----
COPY ./CompositeRunner/ ./Runners/CompositeRunner/
WORKDIR /src/Runners/CompositeRunner
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/CompositeRunner -p:PublishReadyToRun=true -p:SelfContained=false

# ---- DotNetRunner ----
WORKDIR /src
COPY ./DotNetRunner/ ./Runners/DotNetRunner/
WORKDIR /src/Runners/DotNetRunner
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/DotNetRunner -p:PublishReadyToRun=true -p:SelfContained=false

# ---- NodeJsRunner ----
WORKDIR /src
COPY ./NodeJsRunner/ ./Runners/NodeJsRunner/
WORKDIR /src/Runners/NodeJsRunner
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/NodeJsRunner -p:PublishReadyToRun=true -p:SelfContained=false

# ---- TypeScriptRunner ----
WORKDIR /src
COPY ./TypeScriptRunner/ ./Runners/TypeScriptRunner/
WORKDIR /src/Runners/TypeScriptRunner
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/TypeScriptRunner -p:PublishReadyToRun=true -p:SelfContained=false

WORKDIR /src
COPY ./PostgresqlRunner/ ./Runners/PostgresqlRunner/
WORKDIR /src/Runners/PostgresqlRunner/
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/PostgresqlRunner/ -p:PublishReadyToRun=true -p:SelfContained=false

WORKDIR /src
COPY ./MsSqlRunner/ ./Runners/MsSqlRunner/
WORKDIR /src/Runners/MsSqlRunner/
RUN dotnet restore -r linux-x64
RUN dotnet publish -c Release -o /out/MsSqlRunner/ -p:PublishReadyToRun=true -p:SelfContained=false

# ==========================================
# Java build
# ==========================================
FROM maven:3.9.6-eclipse-temurin-21-alpine AS build-java
WORKDIR /app
COPY ./JavaRunner/java-runner/pom.xml ./pom.xml
RUN mvn dependency:go-offline -f pom.xml
COPY ./JavaRunner/java-runner/src ./src
RUN mvn package -DskipTests


# ==========================================
# Kotlin build
# ==========================================
FROM gradle:8.7.0-jdk17-alpine AS build-kotlin
WORKDIR /app
COPY ./KotlinRunner/ ./Runners/KotlinRunner/
WORKDIR /app/Runners/KotlinRunner
RUN gradle shadowJar --no-daemon


# ==========================================
# Final runtime
# ==========================================
FROM alpine:3.20 AS final

RUN apk add --no-cache \
    icu-libs \
    zlib \
    libgcc \
    libstdc++ \
    curl \
    bash \
    nodejs \
    npm \
    openjdk21 \
    openjdk17 \
    && npm install -g typescript \
    && curl -sSL https://dot.net/v1/dotnet-install.sh | bash /dev/stdin -c 9.0 --runtime dotnet --install-dir /usr/share/dotnet

ENV PATH="$PATH:/usr/share/dotnet"

WORKDIR /app/Runners

COPY --from=build-dotnet /out/CompositeRunner ./CompositeRunner
COPY --from=build-dotnet /out/DotNetRunner ./DotNetRunner
COPY --from=build-dotnet /out/NodeJsRunner ./NodeJsRunner
COPY --from=build-dotnet /out/TypeScriptRunner ./TypeScriptRunner
COPY --from=build-dotnet /out/MsSqlRunner ./MsSqlRunner
COPY --from=build-dotnet /out/PostgresqlRunner ./PostgresqlRunner

COPY --from=build-java /app/target/java-runner-1.0-shaded.jar ./JavaRunner/app.jar


COPY --from=build-kotlin /app/Runners/KotlinRunner/build/libs/runner.jar ./KotlinRunner/runner.jar

RUN addgroup -S runneruser && adduser -S -u 1001 runneruser
USER 1001

ENTRYPOINT ["dotnet", "CompositeRunner/CompositeRunner.dll"]


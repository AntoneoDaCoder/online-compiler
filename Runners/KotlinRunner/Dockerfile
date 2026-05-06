FROM gradle:8.7-jdk21-alpine AS kotlin-build

WORKDIR /app/Runners/KotlinRunner

COPY ./build.gradle.kts ./
COPY ./settings.gradle.kts ./
COPY ./gradlew ./
COPY ./gradle ./gradle
COPY ./src ./src

RUN gradle shadowJar --no-daemon


FROM mcr.microsoft.com/dotnet/sdk:10.0 AS supervisor-build
WORKDIR /src

COPY --from=supervisor / "RunnerSupervisor/"
COPY --from=shared / "Shared/"
COPY --from=server-domain / "ServerAPIApp.Domain/"

WORKDIR /src/RunnerSupervisor

RUN dotnet restore -r linux-musl-x64

RUN dotnet publish -c Release -o /app/publish/Supervisor/ \
    -p:PublishReadyToRun=true \
    -p:PublishSingleFile=true \
    -p:SelfContained=true \
    -r linux-musl-x64

FROM eclipse-temurin:21-jre-alpine

WORKDIR /app

RUN apk add --no-cache libgcc libstdc++ icu-libs icu-data-full

RUN addgroup -S runneruser && adduser -S -u 1001 runneruser

COPY --from=kotlin-build /app/Runners/KotlinRunner/build/libs/runner.jar ./runner.jar
COPY --from=supervisor-build /app/publish/Supervisor/RunnerSupervisor .

USER 1001

CMD ["java", "-jar", "runner.jar", "--server"]

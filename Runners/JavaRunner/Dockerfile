# Stage 1: Build with Maven
FROM maven:3.9.6-eclipse-temurin-21-alpine AS build 
RUN adduser -u 1001 -D runneruser

WORKDIR /app
COPY java-runner/pom.xml ./pom.xml

RUN mvn dependency:go-offline -f pom.xml
COPY java-runner/src ./src
RUN mvn package -DskipTests


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


# Stage 2: Runtime
FROM eclipse-temurin:21-jdk-alpine
WORKDIR /app
RUN apk add --no-cache libgcc libstdc++ icu-libs icu-data-full

COPY --from=build /app/target/java-runner-shaded.jar app.jar
COPY --from=supervisor-build /app/publish/Supervisor/RunnerSupervisor .

USER 1001
ENTRYPOINT ["java", "-jar", "app.jar", "--server"]
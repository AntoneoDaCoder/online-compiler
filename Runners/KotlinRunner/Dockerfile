FROM gradle:9.2.1-jdk21-alpine AS build

WORKDIR /app/Runners/KotlinRunner

COPY ./build.gradle.kts ./
COPY ./settings.gradle.kts ./
COPY ./gradlew ./
COPY ./gradle ./gradle
COPY ./src ./src

RUN gradle shadowJar --no-daemon

FROM eclipse-temurin:21-jdk-alpine

WORKDIR /app

RUN addgroup -S runneruser && adduser -S -u 1001 runneruser

COPY --from=build /app/Runners/KotlinRunner/build/libs/runner.jar ./runner.jar

USER 1001

CMD ["java", "-jar", "runner.jar", "--server"]

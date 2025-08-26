FROM gradle:8.7.0-jdk17-alpine AS build

WORKDIR /app/Runners/KotlinRunner

COPY Runners/KotlinRunner/build.gradle.kts ./
COPY Runners/KotlinRunner/settings.gradle.kts ./
COPY Runners/KotlinRunner/gradlew ./
COPY Runners/KotlinRunner/gradle ./gradle
COPY Runners/KotlinRunner/src ./src

RUN ./gradlew shadowJar --no-daemon

FROM eclipse-temurin:20-jre-alpine

WORKDIR /app

RUN addgroup -S runneruser && adduser -S -u 1001 runneruser

COPY --from=build /app/Runners/KotlinRunner/build/libs/runner.jar ./runner.jar

USER 1001

CMD ["java", "-jar", "runner.jar"]

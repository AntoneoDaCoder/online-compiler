# Stage 1: Build with Maven
FROM maven:3.9.6-eclipse-temurin-21-alpine AS build 
RUN adduser -u 1001 -D runneruser
WORKDIR /app
COPY java-runner/pom.xml ./pom.xml
RUN mvn dependency:go-offline -f pom.xml
COPY java-runner/src ./src
RUN mvn package -DskipTests

# Stage 2: Runtime
FROM eclipse-temurin:21-jdk-alpine
WORKDIR /app
COPY --from=build /app/target/java-runner-1.0-shaded.jar app.jar

USER 1001
ENTRYPOINT ["java", "-jar", "app.jar", "--server"]
import org.jetbrains.kotlin.gradle.tasks.KotlinCompile
import org.gradle.jvm.toolchain.JavaLanguageVersion

plugins {
    kotlin("jvm") version "1.9.23"
    application
    id("com.github.johnrengelman.shadow") version "8.1.1"
    kotlin("plugin.serialization") version "1.9.23"
}

group = "org.example"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

dependencies {
    // Kotlin stdlib / compiler
    implementation(kotlin("stdlib"))
    implementation("org.jetbrains.kotlin:kotlin-compiler-embeddable:1.9.23")
    implementation("org.slf4j:slf4j-simple:2.0.12")

    // Jackson (JSON processing used in the code)
    implementation("com.fasterxml.jackson.core:jackson-databind:2.15.2")
    implementation("com.fasterxml.jackson.core:jackson-core:2.15.2")
    implementation("com.fasterxml.jackson.core:jackson-annotations:2.15.2")
    implementation("com.fasterxml.jackson.module:jackson-module-kotlin:2.15.2")
    implementation("com.fasterxml.jackson.datatype:jackson-datatype-jsr310:2.15.2") // OffsetDateTime
    implementation("io.ktor:ktor-serialization-jackson:2.3.4")

    // JUnit needed at runtime (we invoke JUnitCore from generated code)
    implementation("junit:junit:4.13.2")
    implementation("org.hamcrest:hamcrest-core:1.3")

    // Ktor (preserved)
    implementation("io.ktor:ktor-server-cio:2.3.12")
    implementation("io.ktor:ktor-server-content-negotiation:2.3.12")
    implementation("io.ktor:ktor-serialization-kotlinx-json:2.3.12")
    implementation("io.ktor:ktor-server-call-logging:2.3.12")
    implementation("io.ktor:ktor-server-status-pages:2.3.12")

    implementation("io.ktor:ktor-client-core:2.3.12")
    implementation("io.ktor:ktor-client-cio:2.3.12")
    implementation("io.ktor:ktor-client-content-negotiation:2.3.12")

    // Coroutines & Serialization
    implementation("org.jetbrains.kotlinx:kotlinx-coroutines-core:1.8.1")
}

application {
    // поменяй, если у тебя другой main
    mainClass.set("MainKt")
}

// JVM / JDK toolchain: use Java 21
kotlin {
    jvmToolchain(21)
}

java {
    toolchain {
        languageVersion.set(JavaLanguageVersion.of(21))
    }
}

tasks.withType<KotlinCompile> {
    kotlinOptions {
        jvmTarget = "21"
        // additional recommended flags
        freeCompilerArgs = listOf("-Xjsr305=strict")
    }
}

tasks.withType<Jar> {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

tasks.shadowJar {
    archiveBaseName.set("runner")
    archiveClassifier.set("")
    archiveVersion.set("")
}

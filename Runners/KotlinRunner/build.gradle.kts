plugins {
    kotlin("jvm") version "1.9.23"
    application
    id("com.github.johnrengelman.shadow") version "8.1.1"
}

group = "org.example"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

dependencies {
    // сам раннер
    implementation(kotlin("stdlib"))
    implementation("org.jetbrains.kotlin:kotlin-compiler-embeddable:1.9.23")

    // JUnit нужен В РАНТАЙМЕ раннера (мы дергаем JUnitCore из кода)
    implementation("junit:junit:4.13.2")
    implementation("org.hamcrest:hamcrest-core:1.3")
}

application {
    // поменяй, если у тебя другой main
    mainClass.set("MainKt")
}

kotlin {
    // таргет под JVM 17/21 — на докер образ бери тот же
    jvmToolchain(17)
}

// удобная жирная сборка со всеми зависимостями
tasks.withType<Jar> {
    duplicatesStrategy = DuplicatesStrategy.EXCLUDE
}

tasks.shadowJar {
    archiveBaseName.set("runner")
    archiveClassifier.set("")
    archiveVersion.set("")
}

plugins {
    kotlin("jvm") version "1.9.23"
    application
}

group = "org.example"
version = "1.0-SNAPSHOT"

repositories {
    mavenCentral()
}

dependencies {
    implementation("org.jetbrains.kotlin:kotlin-stdlib")
    implementation("org.jetbrains.kotlin:kotlin-compiler-embeddable:1.9.25")
    implementation("junit:junit:4.13.2")
    implementation("org.hamcrest:hamcrest-core:1.3")
}

application {
    mainClass.set("MainKt")
}

tasks.test {
    useJUnitPlatform()
}
kotlin {
    jvmToolchain(20)
}
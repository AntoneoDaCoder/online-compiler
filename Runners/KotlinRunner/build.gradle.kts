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
    implementation("junit:junit:4.13.2")
    implementation(kotlin("stdlib"))
    implementation("org.hamcrest:hamcrest-core:1.3")
    implementation("org.jetbrains.kotlin:kotlin-compiler-embeddable:1.9.23")
    implementation("org.jetbrains.kotlin:kotlin-scripting-compiler-embeddable:1.9.23")
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
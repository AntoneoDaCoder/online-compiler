group "default" {
  targets = ["api", "csharp", "java", "postgresql",/* "swift", */ "nodejs", "kotlin", "typescript"]
}

target "api" {
  dockerfile = "ServerAPIApp/Dockerfile"
  context    = "."
  tags       = ["api-server:local"]
}

target "csharp" {
  dockerfile = "Runners/DotNetRunner/Dockerfile"
  context    = "."
  tags       = ["csharp-runner:local"]
}

target "java" {
  dockerfile = "Runners/JavaRunner/Dockerfile"
  context    = "."
  tags       = ["java-runner:local"]
}

/* target "swift" {
  dockerfile = "Runners/SwiftRunner/Dockerfile"
  context    = "."
  tags       = ["swift-runner:local"]
} */

target "postgresql" {
  dockerfile = "Runners/PostgresqlRunner/Dockerfile"
  context    = "."
  tags       = ["postgresql-runner:local"]
}

target "nodejs" {
  dockerfile = "Runners/NodeJsRunner/Dockerfile"
  context    = "."
  tags       = ["nodejs-runner:local"]
}

target "kotlin" {
  dockerfile = "Runners/KotlinRunner/Dockerfile"
  context    = "."
  tags       = ["kotlin-runner:local"]
}

target "typescript" {
  dockerfile = "Runners/TypeScriptRunner/Dockerfile"
  context    = "."
  tags       = ["typescript-runner:local"]
}

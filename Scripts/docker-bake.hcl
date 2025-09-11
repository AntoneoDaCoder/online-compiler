group "default" {
  targets = ["api", "csharp", "java", "postgresql", "mssql", "nodejs", "kotlin", "typescript"]
}

group "composite" {
  targets = ["api-composite", "composite"]
}

target "api" {
  dockerfile = "Dockerfile"
  context    = "ServerAPIApp/"
  tags       = ["api-server:local"]
  contexts = {
    shared = "Shared/"
    server-core = "ServerAPIApp.Core/"
  }

    args = {
    USE_COMPOSITE = "false"
  }
}

target "api-composite" {
  inherits = ["api"]
  args = {
    USE_COMPOSITE = "true"
  }
}

target "composite" {
  dockerfile = "CompositeRunner/Dockerfile"
  context = "Runners/"
  tags = ["composite-runner:local"]
  contexts = {
    shared = "Shared/"
  }
}


target "csharp" {
  dockerfile = "Dockerfile"
  context    = "Runners/DotNetRunner/"
  tags       = ["csharp-runner:local"]
  contexts = {
    shared = "Shared/",
    runners-shared = "Runners/Runners.Shared/"
  }
}

target  "mssql"{
  dockerfile = "Dockerfile"
  context = "Runners/MsSqlRunner/"
  tags = ["mssql-runner:local"]
  contexts = {
    shared = "Shared/",
    runners-shared = "Runners/Runners.Shared/"
  }
}


target "java" {
  dockerfile = "Dockerfile"
  context    = "Runners/JavaRunner/"
  tags       = ["java-runner:local"]
}

/* target "swift" {
  dockerfile = "Dockerfile"
  context    = "Runners/SwiftRunner/"
  tags       = ["swift-runner:local"]
} */

target "postgresql" {
  dockerfile = "Dockerfile"
  context    = "Runners/PostgresqlRunner/"
  tags       = ["postgresql-runner:local"]
  contexts = {
    shared = "Shared/",
    runners-shared = "Runners/Runners.Shared/"
  }
}

target "nodejs" {
  dockerfile = "Dockerfile"
  context    = "Runners/NodeJsRunner/"
  tags       = ["nodejs-runner:local"]
  contexts = {
    shared = "Shared/",
    runners-shared = "Runners/Runners.Shared/"
  }
}

target "kotlin" {
  dockerfile = "Dockerfile"
  context    = "Runners/KotlinRunner/"
  tags       = ["kotlin-runner:local"]
}

target "typescript" {
  dockerfile = "Dockerfile"
  context    = "Runners/TypeScriptRunner/"
  tags       = ["typescript-runner:local"]
  contexts = {
    shared = "Shared/",
    runners-shared = "Runners/Runners.Shared/"
  }
}

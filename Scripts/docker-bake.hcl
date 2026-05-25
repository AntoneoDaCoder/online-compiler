variable "TAG" {
  default = "dev"
}

group "default" {
  targets = ["api", "csharp", "java", "nodejs", "kotlin", "typescript", "seeder","keycloak"]
}

# group "composite" {
#   targets = ["api-composite", "composite"]
# }

target "api" {
  dockerfile = "Dockerfile"
  context    = "ServerAPIApp/"
  tags       = ["api-server:${TAG}"]
  contexts = {
    shared          = "Shared/"
    server-core     = "ServerAPIApp.Core/"
    server-domain   = "ServerAPIApp.Domain/"
    server-dal      = "ServerAPIApp.DAL/"
    server-contracts = "ServerAPIApp.Contracts/"
  }
}

# target "api-composite" {
#   inherits = ["api"]
#   args = {
#     USE_COMPOSITE = "true"
#   }
# }

# target "composite" {
#   dockerfile = "CompositeRunner/Dockerfile"
#   context    = "Runners/"
#   tags       = ["composite-runner:${TAG}"]
#   contexts = {
#     shared = "Shared/"
#   }
# }

target "seeder" {
  dockerfile = "Dockerfile"
  context    = "DbSeeder/"
  tags       = ["db-seeder:${TAG}"]
  contexts = {
    shared          = "Shared/"
    server-domain   = "ServerAPIApp.Domain/"
    server-dal      = "ServerAPIApp.DAL/"
    data-seed       = "SampleDbSeedingData/"
    server-contracts = "ServerAPIApp.Contracts/"
    server-core     = "ServerAPIApp.Core/"
    appsettings     = "ServerAPIApp/"
  }
}

target "csharp" {
  dockerfile = "Dockerfile"
  context    = "Runners/DotNetRunner/"
  tags       = ["csharp-runner:${TAG}"]
  contexts = {
    shared        = "Shared/"
    runners-shared = "Runners/Runners.Shared/"
    server-domain = "ServerAPIApp.Domain/"
    supervisor = "RunnerSupervisor/"
  }
}

target "java" {
  dockerfile = "Dockerfile"
  context    = "Runners/JavaRunner/"
  tags       = ["java-runner:${TAG}"]
  contexts   = {
    shared        = "Shared/"
    supervisor = "RunnerSupervisor/"
    server-domain = "ServerAPIApp.Domain/"
  }
}

target "nodejs" {
  dockerfile = "Dockerfile"
  context    = "Runners/NodeJsRunner/"
  tags       = ["nodejs-runner:${TAG}"]
  contexts = {
    shared        = "Shared/"
    server-domain = "ServerAPIApp.Domain/"
    runners-shared = "Runners/Runners.Shared/"
    supervisor = "RunnerSupervisor/"
  }
}

target "kotlin" {
  dockerfile = "Dockerfile"
  context    = "Runners/KotlinRunner/"
  tags       = ["kotlin-runner:${TAG}"]
  contexts   = {
    shared        = "Shared/"
    supervisor = "RunnerSupervisor/"
    server-domain = "ServerAPIApp.Domain/"
  }
}

target "typescript" {
  dockerfile = "Dockerfile"
  context    = "Runners/TypeScriptRunner/"
  tags       = ["typescript-runner:${TAG}"]
  contexts = {
    shared        = "Shared/"
    server-domain = "ServerAPIApp.Domain/"
    runners-shared = "Runners/Runners.Shared/"
    supervisor = "RunnerSupervisor/"
  }
}

target "keycloak" {
  dockerfile = "k8s/Dockerfile"
  tags = ["keycloak:${TAG}"]
}
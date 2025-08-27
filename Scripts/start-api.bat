@echo off
echo Starting minikube for API
minikube start --memory=2048

echo Building and loading API image
cd ..
docker build -t api-server:local -f ServerAPIApp/Dockerfile .
minikube image load api-server:local

echo Building and loading csharp runner image
docker build -t csharp-runner:local -f Runners/DotNetRunner/Dockerfile .
minikube image load csharp-runner:local

:: echo Building and loading swift runner image
:: docker build -t swift-runner:local -f Runners/SwiftRunner/Dockerfile .
:: minikube image load swift-runner:local

echo Building and loading java runner image
docker build -t java-runner:local -f Runners/JavaRunner/Dockerfile Runners/JavaRunner/
minikube image load java-runner:local

echo Building and loading postgresql runner image
docker build -t postgresql-runner:local -f Runners/PostgresqlRunner/Dockerfile .
minikube image load postgresql-runner:local

echo Building and loading nodejs runner image
docker build -t nodejs-runner:local -f Runners/NodeJsRunner/Dockerfile .
minikube image load nodejs-runner:local


echo Building and loading typescript runner image
docker build -t typescript-runner:local -f Runners/TypeScriptRunner/Dockerfile .
minikube image load typescript-runner:local

:: --- PostgreSQL setup ---
echo Creating namespace for PostgreSQL
kubectl create ns postgresql

echo Creating secret for PostgreSQL
kubectl -n postgresql create secret generic postgresql ^
  --from-literal=POSTGRES_USER=postgresadmin ^
  --from-literal=POSTGRES_PASSWORD=admin123 ^
  --from-literal=POSTGRES_DB=postgresdb ^
  --from-literal=REPLICATION_USER=replicationuser ^
  --from-literal=REPLICATION_PASSWORD=replicationPassword

echo Deploying PostgreSQL StatefulSet
kubectl -n postgresql apply -f k8s/postgres-statefulset.yaml

echo Waiting for PostgreSQL pod to be ready...
kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=120s


echo Deploying API to Kubernetes
kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/api-deployment.yaml
echo Waiting for pod to be ready...
kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s

start "" cmd /c "kubectl port-forward service/api-server 12345:8080"

echo API is ready to run code
@echo off

echo [93mStarting minikube for API...[0m
minikube start --memory=2048
echo [92mMinikube is ready.[0m

:: echo Building and loading API image
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

echo Building and loading mssql runner image
docker build -t mssql-runner:local -f Runners/MsSqlRunner/Dockerfile .
minikube image load mssql-runner:local

echo Building and loading nodejs runner image
docker build -t nodejs-runner:local -f Runners/NodeJsRunner/Dockerfile .
minikube image load nodejs-runner:local

echo Building and loading kotlin runner image
docker build -t kotlin-runner:local -f Runners/KotlinRunner/Dockerfile .
minikube image load kotlin-runner:local


echo Building and loading typescript runner image
docker build -t typescript-runner:local -f Runners/TypeScriptRunner/Dockerfile .
minikube image load typescript-runner:local

echo [93mLoading images into minikube...[0m
minikube -p minikube image load api-server:local
minikube -p minikube image load csharp-runner:local
minikube -p minikube image load java-runner:local
minikube -p minikube image load postgresql-runner:local
minikube -p minikube image load nodejs-runner:local
minikube -p minikube image load kotlin-runner:local
minikube -p minikube image load typescript-runner:local
:: minikube -p minikube image load swift-runner:local
echo [92mAll images have been loaded[0m

:: --- PostgreSQL setup ---
echo [93mCreating namespace for PostgreSQL...[0m
kubectl create ns postgresql
echo [92mDone![0m

echo [93mCreating secret for PostgreSQL...[0m
kubectl -n postgresql create secret generic postgresql ^
  --from-literal=POSTGRES_USER=postgresadmin ^
  --from-literal=POSTGRES_PASSWORD=admin123 ^
  --from-literal=POSTGRES_DB=postgresdb ^
  --from-literal=REPLICATION_USER=replicationuser ^
  --from-literal=REPLICATION_PASSWORD=replicationPassword
echo [92mDone![0m

echo [93mDeploying PostgreSQL StatefulSet...[0m
kubectl -n postgresql apply -f k8s/postgres-statefulset.yaml
echo [92mDone![0m

echo [93mWaiting for PostgreSQL pod to be ready...[0m
kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=120s
echo [92mDone![0m


echo [93mDeploying API to Kubernetes...[0m

:: --- MSSQL setup ---
echo Creating namespace for MSSQL
kubectl create ns mssql

echo Creating secret for MSSQL
kubectl -n mssql create secret generic mssql ^
  --from-literal=SA_PASSWORD=Admin123! ^
  --from-literal=ACCEPT_EULA=Y ^
  --from-literal=MSSQL_PID=Developer

echo Deploying MSSQL StatefulSet
kubectl -n mssql apply -f k8s/mssql-statefulset.yaml

echo Waiting for MSSQL pod to be ready...
kubectl -n mssql wait --for=condition=ready pod -l app=mssql-server --timeout=120s


echo Deploying API to Kubernetes

kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/api-deployment.yaml
echo [92mDone![0m

echo [93mWaiting for pod to be ready...[0m
kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s
echo [92mDone![0m

start "" cmd /c "kubectl port-forward service/api-server 12345:8080"

echo [92mAPI is ready to run code![0m
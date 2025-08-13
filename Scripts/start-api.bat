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

::docker build -t swift-runner:local -f Runners/SwiftRunner/Dockerfile .
::minikube image load swift-runner:local


echo Building and loading java runner image
docker build -t java-runner:local -f Runners/JavaRunner/Dockerfile Runners/JavaRunner/
minikube image load java-runner:local


echo Building and loading sql runner image
docker build -t sql-runner:local -f Runners/SqlRunner/Dockerfile .
minikube image load sql-runner:local


echo Deploying API to Kubernetes
kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/api-deployment.yaml
echo Waiting for pod to be ready...
kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s


start "" cmd /c "kubectl port-forward service/api-server 12345:8080"

echo API is ready to run code
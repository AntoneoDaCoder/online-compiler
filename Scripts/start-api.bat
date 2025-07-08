@echo off
echo Starting minikube for API
minikube start --memory=2048

echo Building and loading API image
cd ..
docker build -t api-server:local -f ServerAPIApp/Dockerfile .
minikube image load api-server:local

echo Building and loading runner image
docker build -t csharp-runner:local -f InMemoryRunner/Dockerfile .
minikube image load csharp-runner:local

echo Deploying API to Kubernetes
kubectl apply -f k8s/rbac.yaml
kubectl apply -f k8s/api-deployment.yaml
echo Waiting for pod to be ready...
kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s

start "" cmd /c "kubectl port-forward service/api-server 12345:8080"

echo API is ready to run code
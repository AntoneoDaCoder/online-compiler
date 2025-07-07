@echo off
echo Starting minikube for API
minikube start --memory=2048

echo Generating raw kubeconfig
mkdir ..\kubeconfig >nul 2>&1
kubectl config view --flatten --minify --raw > ../kubeconfig/config

if %ERRORLEVEL% NEQ 0 (
    echo Error generating kubeconfig
    exit /b 1
)

echo Replacing localhost with host.docker.internal in kubeconfig

powershell -NoProfile -Command ^
  " $path = '../kubeconfig/config';" ^
  " $content = Get-Content $path;" ^
  " $content = $content -replace 'https://127\.0\.0\.1:(\d+)', 'https://host.docker.internal:$1';" ^
  " Set-Content -Path $path -Value $content"

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

echo API is ready to run code
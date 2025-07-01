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

echo Building runner image
cd ../InMemoryRunner
docker build -t csharp-runner:local .
minikube image load csharp-runner:local
cd ..

echo Starting API container
docker-compose build
docker-compose up -d

echo API is ready to run code
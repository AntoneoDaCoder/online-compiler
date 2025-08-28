@echo off

cd ..
docker buildx bake --file scripts/docker-bake.hcl --progress=plain 2>&1 | findstr /i "ERROR failed error"

if %errorlevel%==0 (
    echo [91mBUILD FAILED[0m
) else (
    echo [92mBUILD SUCCESS[0m
)

echo [93mLoading images into minikube...[0m
minikube -p minikube image load api-server:local
minikube -p minikube image load csharp-runner:local
minikube -p minikube image load java-runner:local
minikube -p minikube image load postgresql-runner:local
minikube -p minikube image load nodejs-runner:local
minikube -p minikube image load kotlin-runner:local
minikube -p minikube image load typescript-runner:local
:: minikube -p minikube image load swift-runner:local
echo [92mAll images have been loaded[0m


exit /b 0

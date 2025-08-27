@echo off
echo Starting parallel builds via Docker Bake...
call minikube -p minikube docker-env
docker buildx bake --progress=plain 2>&1 | findstr /i "ERROR failed error"
exit /b 0

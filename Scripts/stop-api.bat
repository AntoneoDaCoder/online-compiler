@echo off
echo Stopping minikube....
minikube stop

echo Clearing current minikube cluster....
minikube delete

echo Stopping API container
docker-compose down

echo API has been stopped
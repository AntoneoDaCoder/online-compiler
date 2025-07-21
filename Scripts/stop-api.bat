@echo off
echo Stopping minikube....
minikube stop

echo Clearing current minikube cluster....
minikube delete

echo API has been stopped
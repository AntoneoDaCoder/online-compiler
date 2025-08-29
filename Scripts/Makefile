# --- Основные цели ---
.PHONY: up rebuild down bake deploy serve minikube-start

ROOT_DIR := ..

up: minikube-start bake deploy serve

rebuild:
	@echo [93mBuilding images in parallel via Bake...[0m
	@powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File ./rebuild.ps1

down:
	cd $(ROOT_DIR) && \
	kubectl delete -f k8s/api-deployment.yaml && \
    kubectl -n postgresql delete -f k8s/postgres-statefulset.yaml && \
	kubectl delete ns postgresql || true
	@minikube stop

# --- Поднять Minikube ---
minikube-start:
	@echo [93mStarting minikube for API...[0m
	minikube start --memory=2048
	@echo [92mMinikube is ready.[0m


# --- Сборка через Bake с выводом только ошибок ---
bake:
	@echo [93mStarting parallel builds via Docker Bake...[0m
	@bake_wrapper.bat
	@echo [93mLoading images into minikube...[0m
	@minikube -p minikube image load api-server:local
	@minikube -p minikube image load csharp-runner:local
	@minikube -p minikube image load java-runner:local
	@minikube -p minikube image load postgresql-runner:local
	@minikube -p minikube image load nodejs-runner:local
	@minikube -p minikube image load kotlin-runner:local
	@minikube -p minikube image load typescript-runner:local
#   @minikube -p minikube image load swift-runner:local 
	@echo [92mAll images have been loaded[0m

	@echo [92mBuilds finished[0m


# --- Деплой Kubernetes ресурсов ---
deploy: bake
	@echo [93mSetting up Postgre...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns postgresql || true && \
	kubectl -n postgresql delete secret postgresql --ignore-not-found && \
	kubectl -n postgresql create secret generic postgresql \
	--from-literal=POSTGRES_USER=postgresadmin \
	--from-literal=POSTGRES_PASSWORD=admin123 \
	--from-literal=POSTGRES_DB=postgresdb \
	--from-literal=REPLICATION_USER=replicationuser \
	--from-literal=REPLICATION_PASSWORD=replicationPassword
	@echo [92mDone![0m

	@echo [93mDeploying PostgreSQL StatefulSet...[0m
	@cd $(ROOT_DIR) && \
	kubectl -n postgresql apply -f k8s/postgres-statefulset.yaml && \
	kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=120s
	@echo [92mDone![0m

	@echo [93mDeploying API to Kubernetes[0m
	@cd $(ROOT_DIR) && \
	kubectl apply -f k8s/rbac.yaml && \
	kubectl apply -f k8s/api-deployment.yaml && \
	kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s
	@echo [92mDone![0m

# --- Портфорвардинг API ---
serve: deploy
	@echo [93mStarting port-forward to API...[0m
	@port_forward_wrapper.bat
	@echo [92mAPI is ready to run code![0m

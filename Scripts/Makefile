.PHONY: up rebuild down bake deploy serve minikube-start

ROOT_DIR := ..
COMPOSITE ?= 0

up: minikube-start set-composite-config bake deploy serve

rebuild:
	@echo [93mBuilding images in parallel via Bake...[0m
# ifeq ($(COMPOSITE),1)
# 	@powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File ./rebuild.ps1 -Mode composite
# else
	@powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File ./rebuild.ps1 -Mode default
# endif


down:
	@echo [93mStopping minikube....[0m
	@minikube stop

	@echo [93mClearing current minikube cluster....[0m
	@minikube delete

	@echo [92mAPI has been stopped[0m

# --- Поднять Minikube ---
minikube-start:
	@echo [93mStarting minikube for API...[0m
	minikube start --memory=2048
	@echo [92mMinikube is ready.[0m


# --- Сборка через Bake ---
bake:
	@echo [93mStarting parallel builds via Docker Bake...[0m
# ifeq ($(COMPOSITE),1)
# 	@bake_wrapper.bat composite
# 	@minikube -p minikube image load api-server:local
# 	@minikube -p minikube image load composite-runner:local
# else
	@bake_wrapper.bat default
	@minikube -p minikube image load api-server:local
	@minikube -p minikube image load csharp-runner:local
	@minikube -p minikube image load java-runner:local
	@minikube -p minikube image load nodejs-runner:local
	@minikube -p minikube image load kotlin-runner:local
	@minikube -p minikube image load typescript-runner:local
# endif
	@echo [92mAll images have been loaded[0m
	@echo [92mBuilds finished[0m


# --- Деплой Kubernetes ресурсов ---
deploy: bake set-composite-config
	@echo [93mSetting up PostgreSQL...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns postgresql || true && \
	kubectl -n postgresql delete secret postgres-secret --ignore-not-found && \
	kubectl -n postgresql create secret generic postgres-secret \
	--from-literal=POSTGRES_USER=postgresadmin \
	--from-literal=POSTGRES_PASSWORD=admin123 \
	--from-literal=POSTGRES_DB=postgresdb \
	--from-literal=POSTGRES_REPLICATION_USER=replicationuser \
	--from-literal=POSTGRES_REPLICATION_PASSWORD=replicationPassword
	@echo [92mPostgreSQL secret created![0m

	@echo [93mDeploying PostgreSQL StatefulSet...[0m
	@cd $(ROOT_DIR) && \
	kubectl -n postgresql apply -f k8s/postgres-config.yaml && \
	kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=120s
	@echo [92mPostgreSQL deployed and ready![0m
	
	@echo [93mSetting up MinIO...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns minio || true && \
	kubectl -n minio delete secret minio-secret --ignore-not-found && \
	kubectl -n minio create secret generic minio-secret \
	--from-literal=MINIO_ROOT_USER=minioadmin \
	--from-literal=MINIO_ROOT_PASSWORD=minioadmin123 \
	--from-literal=ACCESS_KEY=minioadmin \
	--from-literal=SECRET_KEY=minioadmin123
	@echo [92mMinIO secret created![0m

	@echo [93mDeploying MinIO StatefulSet...[0m
	@cd $(ROOT_DIR) && \
	kubectl -n minio apply -f k8s/minio-config.yaml && \
	kubectl -n minio wait --for=condition=ready pod -l app=minio --timeout=120s
	@echo [92mMinIO deployed and ready![0m

	@echo [93mChecking all dependencies are ready...[0m
	@cd $(ROOT_DIR) && \
	echo "Checking PostgreSQL..." && \
	kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=30s && \
	echo "PostgreSQL is ready!" && \
	echo "Checking MinIO..." && \
	kubectl -n minio wait --for=condition=ready pod -l app=minio --timeout=30s && \
	echo "MinIO is ready!"
	@echo [92mAll dependencies are ready![0m
	
	@echo [93mDeploying API to Kubernetes[0m
	@cd $(ROOT_DIR) && \
	kubectl apply -f k8s/rbac.yaml && \
	kubectl apply -f k8s/api-deployment.yaml && \
	kubectl rollout restart deployment api-server -n default && \
	kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s

	@echo [92mAPI deployed successfully![0m


# --- Портфорвардинг API ---
serve: deploy
	@echo [93mStarting port-forward to API...[0m
	@port_forward_wrapper.bat
	@echo [92mAPI is ready to run code![0m


set-composite-config:
# ifeq ($(COMPOSITE),1)
# 	@kubectl create configmap api-config --from-literal=UseComposite=true --dry-run=client -o yaml | kubectl apply -f -
# else
	@kubectl create configmap api-config --from-literal=UseComposite=false --dry-run=client -o yaml | kubectl apply -f -
# endif
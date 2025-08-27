# --- Основные цели ---
.PHONY: up rebuild down bake deploy serve minikube-start

ROOT_DIR := ..

up: minikube-start bake deploy serve

rebuild: bake
	kubectl rollout restart deployment api-server

down:
	-cd $(ROOT_DIR) && kubectl delete -f k8s/api-deployment.yaml
	-cd $(ROOT_DIR) && kubectl -n postgresql delete -f k8s/postgres-statefulset.yaml
	-cd $(ROOT_DIR) && kubectl delete ns postgresql || true
	@minikube stop

# --- Поднять Minikube ---
minikube-start:
	@echo "Starting Minikube..."
	cd $(ROOT_DIR) && minikube start --memory=2048
	@echo "Minikube is ready."


# --- Сборка через Bake с выводом только ошибок ---
bake:
	@echo "Starting parallel builds via Docker Bake..."
	cd $(ROOT_DIR)/scripts && ./bake_wrapper.bat
	@echo "Builds finished (check above for errors)"


# --- Деплой Kubernetes ресурсов ---
deploy:
	@echo "Setting up PostgreSQL..."
	cd $(ROOT_DIR) && kubectl create ns postgresql || true
	cd $(ROOT_DIR) && kubectl -n postgresql delete secret postgresql --ignore-not-found
	cd $(ROOT_DIR) && kubectl -n postgresql create secret generic postgresql \
	  --from-literal=POSTGRES_USER=postgresadmin \
	  --from-literal=POSTGRES_PASSWORD=admin123 \
	  --from-literal=POSTGRES_DB=postgresdb \
	  --from-literal=REPLICATION_USER=replicationuser \
	  --from-literal=REPLICATION_PASSWORD=replicationPassword
	@echo "Deploying PostgreSQL StatefulSet..."
	cd $(ROOT_DIR) && kubectl -n postgresql apply -f k8s/postgres-statefulset.yaml
	cd $(ROOT_DIR) && kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=120s

	@echo "Deploying API..."
	cd $(ROOT_DIR) && kubectl apply -f k8s/rbac.yaml
	cd $(ROOT_DIR) && kubectl apply -f k8s/api-deployment.yaml
	cd $(ROOT_DIR) && kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s

# --- Портфорвардинг API ---
serve:
	@echo "Starting port-forward to API..."
	cd $(ROOT_DIR) && start "" cmd /c "kubectl port-forward service/api-server 12345:8080"
	@echo "API is ready to run code"

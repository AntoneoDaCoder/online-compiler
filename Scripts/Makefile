.PHONY: up rebuild down bake deploy serve minikube-start backup-db restore-db
ROOT_DIR := ..
COMPOSITE ?= 0
FIRST_RUN ?= 0

up: minikube-start set-composite-config bake deploy serve

rebuild:
	@echo [93mBuilding images in parallel via Bake...[0m
	@powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File ./rebuild.ps1 -Mode default

down: backup-db
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
ifeq ($(COMPOSITE),1)
	@bake_wrapper.bat composite
	@minikube -p minikube image load api-server:local
	@minikube -p minikube image load composite-runner:local
else
	@bake_wrapper.bat default
	@minikube -p minikube image load api-server:local
	@minikube -p minikube image load csharp-runner:local
	@minikube -p minikube image load java-runner:local
	@minikube -p minikube image load nodejs-runner:local
	@minikube -p minikube image load kotlin-runner:local
	@minikube -p minikube image load typescript-runner:local
endif
	@minikube -p minikube image load postgres:latest
	@minikube -p minikube image load minio/minio:latest
	@minikube -p minikube image load quay.io/keycloak/keycloak:latest
#В режиме composite всегда грузим db-seeder, если FIRST_RUN=1
ifeq ($(FIRST_RUN),1)
	@minikube -p minikube image load db-seeder:local
endif
	@echo [92mAll images have been loaded[0m
	@echo [92mBuilds finished[0m

# --- Деплой Kubernetes ресурсов ---
deploy: bake set-composite-config
	@kubectl config current-context
	@kubectl get ns

	@echo [93mSetting up PostgreSQL...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns postgresql --dry-run=client -o yaml | kubectl apply -f - && \
	kubectl -n postgresql delete secret postgres-secret --ignore-not-found && \
	kubectl -n postgresql create secret generic postgres-secret \
  		--from-literal=POSTGRES_USER=postgresadmin \
  		--from-literal=POSTGRES_PASSWORD=admin123 && \
	kubectl -n postgresql apply -f k8s/postgres-statefulset.yaml && \
	kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=300s
	@echo [92mPostgreSQL deployed and ready![0m
ifeq ($(FIRST_RUN),1)
	@echo [93mFIRST_RUN=1 → skipping backup restore[0m
	@echo [93mEnsuring keycloakdb exists for fresh setup...[0m
	@kubectl -n postgresql exec postgres-0 -- bash -c "PGPASSWORD=admin123 psql -U postgresadmin -d postgres -tc \"SELECT 1 FROM pg_database WHERE datname = 'keycloakdb'\" | grep -q 1 || PGPASSWORD=admin123 psql -U postgresadmin -d postgres -c \"CREATE DATABASE keycloakdb\""
else
	@$(MAKE) restore-db
	@echo [93mEnsuring keycloakdb exists if no backup was restored...[0m
	@kubectl -n postgresql exec postgres-0 -- bash -c "PGPASSWORD=admin123 psql -U postgresadmin -d postgres -tc \"SELECT 1 FROM pg_database WHERE datname = 'keycloakdb'\" | grep -q 1 || PGPASSWORD=admin123 psql -U postgresadmin -d postgres -c \"CREATE DATABASE keycloakdb\""
endif

	@echo [93mSetting up Keycloak...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns keycloak --dry-run=client -o yaml | kubectl apply -f - && \
	kubectl -n keycloak delete secret keycloak-admin-secret keycloak-db-secret --ignore-not-found && \
	kubectl -n keycloak create secret generic keycloak-admin-secret \
	  --from-literal=KC_BOOTSTRAP_ADMIN_USERNAME=admin \
	  --from-literal=KC_BOOTSTRAP_ADMIN_PASSWORD=admin123 && \
	kubectl -n keycloak create secret generic keycloak-db-secret \
	  --from-literal=KC_DB_USERNAME=postgresadmin \
	  --from-literal=KC_DB_PASSWORD=admin123 && \
	kubectl -n keycloak apply -f k8s/keycloak.yaml && \
	kubectl -n keycloak rollout status deploy/keycloak --timeout=300s
	@echo [92mKeycloak deployed and ready![0m
	
	@echo [93mSetting up MinIO...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns minio || true && \
	kubectl -n minio delete secret minio-secret --ignore-not-found && \
	kubectl -n minio create secret generic minio-secret \
	  --from-literal=MINIO_ROOT_USER=minioadmin \
	  --from-literal=MINIO_ROOT_PASSWORD=minioadmin123 &&\
	kubectl -n minio apply -f k8s/minio-statefulset.yaml && \
	kubectl -n minio wait --for=condition=ready pod -l app=minio --timeout=120s
	@echo [92mMinIO deployed and ready![0m

	@echo [93mChecking all dependencies are ready...[0m
	@cd $(ROOT_DIR) && \
	echo [92mChecking PostgreSQL...[0m && \
	kubectl -n postgresql wait --for=condition=ready pod -l app=postgres --timeout=30s && \
	echo "PostgreSQL is ready!" && \
	echo [93mChecking MinIO...[0m && \
	kubectl -n minio wait --for=condition=ready pod -l app=minio --timeout=30s && \
	echo  [92mMinIO is ready![0m
	@echo [92mAll dependencies are ready![0m

ifeq ($(FIRST_RUN),1)
	@echo [93mRunning db-seeder job..[0m
	@cd $(ROOT_DIR) && \
	kubectl delete job db-seeder --ignore-not-found && \
	kubectl -n postgresql delete secret seeder-secret --ignore-not-found && \
	kubectl -n postgresql create secret generic seeder-secret \
  	  --from-literal=ACCESS_KEY=minioadmin \
  	  --from-literal=SECRET_KEY=minioadmin123 \
	  --from-literal=ConnectionString="Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=onlinecompilerdb;Username=postgresadmin;Password=admin123" \
	  --from-literal=ApiClientSecret=unHsofo6LqyFWYiLFw8xynTdFhGM1grs && \
	kubectl -n postgresql apply -f k8s/db-seeder-job.yaml && \
	kubectl -n postgresql wait --for=condition=complete --timeout=300s job/db-seeder && \
	echo [92mdb-seeder job completed successfully![0m || \
	(echo [91mdb-seeder job failed![0m && kubectl logs job/db-seeder && exit 1)
else
	@echo [93mSkipping db-seeder job...[0m
endif
	@echo [93mDeploying API to Kubernetes[0m
	@cd $(ROOT_DIR) && \
	kubectl delete secret api-secret --ignore-not-found && \
	kubectl create secret generic api-secret \
		--from-literal=DbConnectionString="Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=onlinecompilerdb;Username=postgresadmin;Password=admin123" \
		--from-literal=AccessKey=minioadmin \
		--from-literal=SecretKey=minioadmin123 \
		--from-literal=ApiClientSecret=unHsofo6LqyFWYiLFw8xynTdFhGM1grs &&\
	kubectl apply -f k8s/rbac.yaml && \
	kubectl apply -f k8s/api-deployment.yaml && \
	kubectl rollout restart deployment api-server -n default && \
	kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s && \
	echo [92mAPI deployed successfully![0m

# --- Портфорвардинг API ---
serve: deploy
	@echo [93mStarting port-forward to API...[0m
	@port_forwarder.bat
	@echo [92mPort forwarding is active[0m
ifeq ($(FIRST_RUN),0)
	@echo [93mRestoring Minio from the backup folder...[0m
	@bash restore-minio.sh
	@echo [92mMinio data has been restored![0m
endif
	@echo [92mAPI is ready to run code![0m

set-composite-config:
ifeq ($(COMPOSITE),1)
	@kubectl create configmap api-config --from-literal=UseComposite=true --dry-run=client -o yaml | kubectl apply -f -
else
	@kubectl create configmap api-config --from-literal=UseComposite=false --dry-run=client -o yaml | kubectl apply -f -
endif

backup-db:
	@echo [93mBacking up PostgreSQL before cluster shutdown...[0m
	@bash backup-db.sh || true
	@echo [92mPostgreSQL backup step finished.[0m
	@echo [93mBacking up Minio before cluster shutdown...[0m
	@bash minio-backup.sh
	@echo [92mMinio backup step finished.[0m

restore-db:
	@echo [93mChecking for PostgreSQL backup and restoring if present...[0m
	@bash restore-db.sh; \
	RESTORE_EXIT=$$?; \
	if [ $$RESTORE_EXIT -eq 0 ]; then \
		echo [92mPostgreSQL restore completed.[0m; \
	elif [ $$RESTORE_EXIT -eq 10 ]; then \
		echo [93mNo backup found, skipping restore.[0m; \
	else \
		echo [91mRestore failed![0m; \
		exit 1; \
	fi
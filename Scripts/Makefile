.PHONY: up dev first-run rebuild down delete-cluster bake deploy-infra deploy-api serve minikube-start set-composite-config seed-db backup-db restore-db

ROOT_DIR := ..
MINIKUBE_PROFILE ?= minikube
COMPOSITE ?= 0
FIRST_RUN ?= 0
TAG ?= dev

up: minikube-start set-composite-config bake deploy-infra deploy-api serve
dev: up

first-run: minikube-start set-composite-config bake deploy-infra deploy-api seed-db serve

rebuild:
	@echo [93mBuilding images in parallel via Bake...[0m
	@powershell -NoLogo -NoProfile -ExecutionPolicy Bypass -File ./rebuild.ps1 -Tag $(TAG)

down: backup-db
	@echo [93mStopping minikube....[0m
	@minikube stop -p $(MINIKUBE_PROFILE)
	@echo [92mAPI has been stopped[0m

delete-cluster: backup-db
	@echo [93mDeleting minikube cluster....[0m
	@minikube delete -p $(MINIKUBE_PROFILE)
	@echo [92mCluster deleted[0m

minikube-start:
	@echo [93mStarting minikube for API...[0m
	@minikube start -p $(MINIKUBE_PROFILE) --memory=4096 --cpus=4
	@echo [92mMinikube is ready.[0m

bake:
	@echo [93mStarting parallel builds via Docker Bake...[0m
	@set TAG=$(TAG)
	@bake_wrapper.bat default
	@powershell -ExecutionPolicy Bypass -File image-loader.ps1 -TAG $(TAG) -MINIKUBE_PROFILE $(MINIKUBE_PROFILE) -COMPOSITE $(COMPOSITE) -FIRST_RUN $(FIRST_RUN)

deploy-infra:
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
	@echo [93mCreating Keycloak database schema...[0m
	@kubectl -n postgresql exec postgres-0 -- bash -c "PGPASSWORD=admin123 psql -U postgresadmin -d postgres -tc \"SELECT 1 FROM pg_database WHERE datname = 'keycloakdb'\" | grep -q 1 || PGPASSWORD=admin123 psql -U postgresadmin -d postgres -c \"CREATE DATABASE keycloakdb\""
	@echo [92mKeycloak database has been created![0m
else
	$(MAKE) restore-db
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
	kubectl -n keycloak wait --for=condition=ready pod -l app=keycloak --timeout=300s
	@echo [92mKeycloak deployed and ready![0m

	@echo [93mSetting up MinIO...[0m
	@cd $(ROOT_DIR) && \
	kubectl create ns minio --dry-run=client -o yaml | kubectl apply -f - && \
	kubectl -n minio delete secret minio-secret --ignore-not-found && \
	kubectl -n minio create secret generic minio-secret \
		--from-literal=MINIO_ROOT_USER=minioadmin \
		--from-literal=MINIO_ROOT_PASSWORD=minioadmin123 && \
	kubectl -n minio apply -f k8s/minio-statefulset.yaml && \
	kubectl -n minio wait --for=condition=ready pod -l app=minio --timeout=120s
	@echo [92mMinIO deployed and ready![0m

deploy-api:
	@echo [93mDeploying API to Kubernetes[0m
	@cd $(ROOT_DIR) && \
	kubectl delete secret api-secret --ignore-not-found && \
	kubectl create secret generic api-secret \
		--from-literal=DbConnectionString="Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=onlinecompilerdb;Username=postgresadmin;Password=admin123" \
		--from-literal=AccessKey=minioadmin \
		--from-literal=SecretKey=minioadmin123 \
		--from-literal=ApiClientSecret=GWAz8Um7nsGsb4Hzl7C49ib5t6CUo5AR && \
	kubectl apply -f k8s/rbac.yaml && \
	kubectl apply -f k8s/api-deployment.yaml && \
	kubectl rollout restart deployment api-server -n default && \
	kubectl wait --for=condition=ready pod -l app=api-server --timeout=90s && \
	echo [92mAPI deployed successfully![0m

serve:
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

seed-db:
ifeq ($(FIRST_RUN),1)
	@echo [93mRunning db-seeder job..[0m
	@cd $(ROOT_DIR) && \
	kubectl delete job db-seeder --ignore-not-found && \
	kubectl -n postgresql delete secret seeder-secret --ignore-not-found && \
	kubectl -n postgresql create secret generic seeder-secret \
		--from-literal=ACCESS_KEY=minioadmin \
		--from-literal=SECRET_KEY=minioadmin123 \
		--from-literal=ConnectionString="Host=postgres.postgresql.svc.cluster.local;Port=5432;Database=onlinecompilerdb;Username=postgresadmin;Password=admin123" \
		--from-literal=ApiClientSecret=GWAz8Um7nsGsb4Hzl7C49ib5t6CUo5AR && \
	kubectl -n postgresql apply -f k8s/db-seeder-job.yaml && \
	kubectl -n postgresql wait --for=condition=complete --timeout=300s job/db-seeder && \
	echo [92mdb-seeder job completed successfully![0m || \
	(echo [91mdb-seeder job failed![0m && kubectl logs job/db-seeder && exit 1)
else
	@echo [93mSkipping db-seeder job...[0m
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
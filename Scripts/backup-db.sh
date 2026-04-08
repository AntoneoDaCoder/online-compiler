#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="postgresql"
POD_NAME="postgres-0"
DB_USER="postgresadmin"
DB_PASSWORD="admin123"
BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Postgres"

mkdir -p "$BACKUP_DIR"

LATEST_BACKUP="$BACKUP_DIR/full-postgres-backup-latest.sql"

echo "Checking if PostgreSQL pod exists..."
if ! kubectl get pod "$POD_NAME" -n "$NAMESPACE" >/dev/null 2>&1; then
  echo "PostgreSQL pod not found. Skipping backup."
  exit 0
fi

echo "Checking if PostgreSQL is ready..."
if ! kubectl wait --for=condition=ready pod/"$POD_NAME" -n "$NAMESPACE" --timeout=30s >/dev/null 2>&1; then
  echo "PostgreSQL pod is not ready. Skipping backup."
  exit 0
fi

echo "Creating full PostgreSQL cluster backup..."
kubectl exec -n "$NAMESPACE" "$POD_NAME" -- \
  bash -c "PGPASSWORD=$DB_PASSWORD pg_dumpall -U $DB_USER --clean --if-exists" \
  > "$LATEST_BACKUP"

echo "Backup completed successfully: $LATEST_BACKUP"
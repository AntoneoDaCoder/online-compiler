#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="postgresql"
POD_NAME="postgres-0"
DB_USER="postgresadmin"
DB_PASSWORD="admin123"
BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Postgres"

# Находим самый новый файл бэкапа
LATEST_BACKUP=$(ls -t "$BACKUP_DIR"/backup-*.sql 2>/dev/null | head -1)

if [ -z "$LATEST_BACKUP" ] || [ ! -f "$LATEST_BACKUP" ]; then
  echo "No backup file found in $BACKUP_DIR (pattern: backup-*.sql). Skipping restore."
  exit 10
fi

echo "Using backup: $LATEST_BACKUP"

echo "Waiting for PostgreSQL pod..."
kubectl wait --for=condition=ready pod/"$POD_NAME" -n "$NAMESPACE" --timeout=300s

echo "Restoring full PostgreSQL cluster backup..."
kubectl exec -i -n "$NAMESPACE" "$POD_NAME" -- \
  bash -c "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d postgres" \
  < "$LATEST_BACKUP"

echo "Restore completed successfully."
exit 0
#!/usr/bin/env bash
set -euo pipefail

NAMESPACE="postgresql"
POD_NAME="postgres-0"
DB_USER="postgresadmin"
DB_PASSWORD="admin123"
BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Postgres"

LATEST_BACKUP="$BACKUP_DIR/full-postgres-backup-latest.sql"

if [ ! -f "$LATEST_BACKUP" ]; then
  echo "No backup found. Skipping restore."
  exit 10
fi

echo "Waiting for PostgreSQL pod..."
kubectl wait --for=condition=ready pod/"$POD_NAME" -n "$NAMESPACE" --timeout=300s

echo "Restoring full PostgreSQL cluster backup..."
kubectl exec -i -n "$NAMESPACE" "$POD_NAME" -- \
  bash -c "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d postgres" \
  < "$LATEST_BACKUP"

echo "Restore completed successfully."
exit 0
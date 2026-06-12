#!/usr/bin/env bash
set -euo pipefail

set -a
. ./secrets.env
set +a

NAMESPACE="postgresql"
POD_NAME="postgres-0"
DB_USER=$DbUser
DB_PASSWORD=$DbPass

BASE_BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Postgres"
KEYCLOAK_DIR="$BASE_BACKUP_DIR/Keycloak"
COMPILER_DIR="$BASE_BACKUP_DIR/CompilerData"

mkdir -p "$KEYCLOAK_DIR" "$COMPILER_DIR"

TS="$(date +%Y%m%d_%H%M%S)"
KEYCLOAK_FILE="$KEYCLOAK_DIR/keycloak-$TS.sql"
COMPILER_FILE="$COMPILER_DIR/compiler-data-$TS.sql"

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

echo "Creating Keycloak backup..."
kubectl exec -n "$NAMESPACE" "$POD_NAME" -- \
  bash -lc "PGPASSWORD='$DB_PASSWORD' pg_dump -U '$DB_USER' --clean --if-exists --no-owner --no-privileges keycloakdb" \
  > "$KEYCLOAK_FILE"

echo "Creating CompilerData backup..."
kubectl exec -n "$NAMESPACE" "$POD_NAME" -- \
  bash -lc "PGPASSWORD='$DB_PASSWORD' pg_dumpall -U '$DB_USER' --clean --if-exists --exclude-database=keycloakdb" \
  > "$COMPILER_FILE"

echo "Backups completed successfully:"
echo "  Keycloak:    $KEYCLOAK_FILE"
echo "  CompilerData: $COMPILER_FILE"
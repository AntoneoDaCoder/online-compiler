#!/usr/bin/env bash
set -euo pipefail

set -a
. ./secrets.env
set +a

NAMESPACE="postgresql"
POD_NAME="postgres-0"
DB_USER=$DbUser
DB_PASSWORD=$DbPass
FIRST_RUN="${FIRST_RUN:-0}"

BASE_BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Postgres"
KEYCLOAK_DIR="$BASE_BACKUP_DIR/Keycloak"
COMPILER_DIR="$BASE_BACKUP_DIR/CompilerData"

LATEST_KEYCLOAK_BACKUP="$(find "$KEYCLOAK_DIR" -maxdepth 1 -type f -name 'keycloak-*.sql' 2>/dev/null | sort | tail -1 || true)"
LATEST_COMPILER_BACKUP="$(find "$COMPILER_DIR" -maxdepth 1 -type f -name 'compiler-data-*.sql' 2>/dev/null | sort | tail -1 || true)"

db_exists() {
  local db_name="$1"
  local result

  result="$(kubectl exec -n "$NAMESPACE" "$POD_NAME" -- \
    bash -lc "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d postgres -tAc \"SELECT 1 FROM pg_database WHERE datname='${db_name}'\"" \
    | tr -d '[:space:]')"

  [ "$result" = "1" ]
}

create_db() {
  local db_name="$1"

  if ! db_exists "$db_name"; then
    echo "Creating database: $db_name"
    kubectl exec -n "$NAMESPACE" "$POD_NAME" -- \
      bash -lc "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d postgres -c \"CREATE DATABASE ${db_name};\""
  fi
}

restore_keycloak_db() {
  if [ -z "$LATEST_KEYCLOAK_BACKUP" ] || [ ! -f "$LATEST_KEYCLOAK_BACKUP" ]; then
    echo "No Keycloak backup found, creating empty keycloakdb..."
    create_db "keycloakdb"
    return 0
  fi

  echo "Restoring Keycloak backup: $LATEST_KEYCLOAK_BACKUP"
  create_db "keycloakdb"

  if ! kubectl exec -i -n "$NAMESPACE" "$POD_NAME" -- \
    bash -lc "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d keycloakdb" \
    < "$LATEST_KEYCLOAK_BACKUP"; then
    echo "Keycloak restore failed, creating empty keycloakdb..."
    create_db "keycloakdb"
  fi
}

restore_compiler_data() {
  if [ -z "$LATEST_COMPILER_BACKUP" ] || [ ! -f "$LATEST_COMPILER_BACKUP" ]; then
    echo "No CompilerData backup found, skipping restore."
    return 10
  fi

  echo "Restoring CompilerData backup: $LATEST_COMPILER_BACKUP"
  kubectl exec -i -n "$NAMESPACE" "$POD_NAME" -- \
    bash -lc "PGPASSWORD=$DB_PASSWORD psql -U $DB_USER -d postgres" \
    < "$LATEST_COMPILER_BACKUP"
}

echo "Waiting for PostgreSQL pod..."
kubectl wait --for=condition=ready pod/"$POD_NAME" -n "$NAMESPACE" --timeout=300s

if [ "$FIRST_RUN" = "1" ]; then
  echo "FIRST_RUN=1: restoring only Keycloak database if backup exists."
  restore_keycloak_db
  echo "First-run restore step completed."
  exit 0
fi

echo "Restoring full PostgreSQL data..."
COMPILER_RC=0
restore_compiler_data || COMPILER_RC=$?

restore_keycloak_db

if [ "$COMPILER_RC" -eq 10 ]; then
  echo "CompilerData backup was not found; skipped."
fi

echo "Restore completed successfully."
exit 0
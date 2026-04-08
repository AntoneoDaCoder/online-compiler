#!/usr/bin/env bash
set -euo pipefail

MINIO_URL="${MINIO_URL:-http://host.docker.internal:9000}"
BUCKET="${BUCKET:-manifestbucket}"
SOURCE_DIR="${SOURCE_DIR:-/c/OnlineCompilerMinikubeVolumes/Minio}"
SOURCE_DIR_WIN="$(cygpath -am "$SOURCE_DIR")"

MINIO_ALIAS="${MINIO_ALIAS:-myminio}"
MINIO_ROOT_USER="${MINIO_ROOT_USER:-minioadmin}"
MINIO_ROOT_PASSWORD="${MINIO_ROOT_PASSWORD:-minioadmin123}"

sleep 5

if [[ ! -d "${SOURCE_DIR}" ]]; then
  echo "Source directory not found: ${SOURCE_DIR}"
  exit 1
fi

mkdir -p "${SOURCE_DIR}"

echo "Restoring MinIO from local backup..."
MSYS_NO_PATHCONV=1 docker run --rm \
  --entrypoint /bin/sh \
  --add-host=host.docker.internal:host-gateway \
  -e MINIO_URL="$MINIO_URL" \
  -e BUCKET="$BUCKET" \
  -e MINIO_ALIAS="$MINIO_ALIAS" \
  -e MINIO_ROOT_USER="$MINIO_ROOT_USER" \
  -e MINIO_ROOT_PASSWORD="$MINIO_ROOT_PASSWORD" \
  -v "${SOURCE_DIR_WIN}:/restore" \
  minio/mc -ec '
    mc alias set "$MINIO_ALIAS" "$MINIO_URL" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"
    mc ls "$MINIO_ALIAS/$BUCKET" >/dev/null
    mc mirror --overwrite /restore "$MINIO_ALIAS/$BUCKET"
  '

echo "Done"
#!/usr/bin/env bash
set -euo pipefail

set -a
. ./secrets.env
set +a

MINIO_URL="${MINIO_URL:-http://host.docker.internal:9000}"
BUCKET=$ManifestBucket
SOURCE_DIR="${SOURCE_DIR:-/c/OnlineCompilerMinikubeVolumes/Minio}"

MINIO_ALIAS=$MinioAlias
MINIO_ROOT_USER=$AccessKey
MINIO_ROOT_PASSWORD=$SecretKey

if ! command -v cygpath >/dev/null 2>&1; then
  echo "cygpath not found"
  exit 1
fi

if [[ ! -d "${SOURCE_DIR}" ]]; then
  echo "Source directory not found: ${SOURCE_DIR}"
  exit 1
fi

SOURCE_DIR_WIN="$(cygpath -am "$SOURCE_DIR")"

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
    set -eu

    until mc alias set "$MINIO_ALIAS" "$MINIO_URL" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD" >/dev/null 2>&1; do
      echo "Waiting for MinIO..."
      sleep 2
    done

    mc mb --ignore-existing "$MINIO_ALIAS/$BUCKET"
    mc mirror --overwrite --remove /restore "$MINIO_ALIAS/$BUCKET"
  '

echo "Done"
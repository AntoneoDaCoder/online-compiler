#!/usr/bin/env bash
set -euo pipefail

set -a
. ./secrets.env
set +a

MINIO_URL="${MINIO_URL:-http://host.docker.internal:9000}"
BUCKET=$ManifestBucket
BACKUP_DIR="/c/OnlineCompilerMinikubeVolumes/Minio"

MINIO_ALIAS=$MinioAlias
MINIO_ROOT_USER=$AccessKey
MINIO_ROOT_PASSWORD=$SecretKey

mkdir -p "${BACKUP_DIR}"

MSYS_NO_PATHCONV=1 docker run --rm \
  --entrypoint /bin/sh \
  --add-host=host.docker.internal:host-gateway \
  -e MINIO_URL="$MINIO_URL" \
  -e BUCKET="$BUCKET" \
  -e MINIO_ALIAS="$MINIO_ALIAS" \
  -e MINIO_ROOT_USER="$MINIO_ROOT_USER" \
  -e MINIO_ROOT_PASSWORD="$MINIO_ROOT_PASSWORD" \
  -v "${BACKUP_DIR}:/backup" \
  minio/mc:latest -ec '
    echo "MINIO_ALIAS=$MINIO_ALIAS"
    echo "MINIO_URL=$MINIO_URL"
    echo "BUCKET=$BUCKET"

    mc alias set "$MINIO_ALIAS" "$MINIO_URL" "$MINIO_ROOT_USER" "$MINIO_ROOT_PASSWORD"
    mc ls "$MINIO_ALIAS/$BUCKET" >/dev/null
    mc mirror --overwrite "$MINIO_ALIAS/$BUCKET" /backup
  '

echo "Done"
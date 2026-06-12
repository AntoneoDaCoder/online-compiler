#!/bin/sh

# Переход в родительскую директорию относительно расположения скрипта
SCRIPT_DIR=$(CDPATH= cd -- "$(dirname -- "$0")" && pwd)
cd "$SCRIPT_DIR/.." || exit 1

MODE=${1:-}

if [ "$MODE" = "composite" ]; then
    echo "Building in COMPOSITE mode..."
    docker buildx bake --file scripts/docker-bake.hcl composite --progress=plain
else
    echo "Building in DEFAULT mode..."
    docker buildx bake --file scripts/docker-bake.hcl default --progress=plain
fi

if [ $? -ne 0 ]; then
    printf '\033[91mBUILD FAILED\033[0m\n'
else
    printf '\033[92mBUILD SUCCESS\033[0m\n'
fi

exit 0
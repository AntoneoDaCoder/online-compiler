#!/bin/sh

TAG=${TAG:-${1:-}}
MINIKUBE_PROFILE=${MINIKUBE_PROFILE:-${2:-}}
COMPOSITE=${COMPOSITE:-${3:-0}}
FIRST_RUN=${FIRST_RUN:-${4:-0}}

if [ -z "$TAG" ] || [ -z "$MINIKUBE_PROFILE" ]; then
    echo "TAG and MINIKUBE_PROFILE are required"
    exit 1
fi

images_file=$(mktemp)
jobs_file=$(mktemp)

cleanup() {
    rm -f "$images_file" "$jobs_file"
}
trap cleanup EXIT INT TERM

add_image() {
    printf '%s\n' "$1" >> "$images_file"
}

add_image "api-server:$TAG"
add_image "postgres:latest"
add_image "minio/minio:latest"
add_image "minio/mc:latest"
add_image "keycloak:$TAG"

if [ "$COMPOSITE" -eq 1 ]; then
    add_image "composite-runner:$TAG"
else
    add_image "csharp-runner:$TAG"
    add_image "java-runner:$TAG"
    add_image "nodejs-runner:$TAG"
    add_image "kotlin-runner:$TAG"
    add_image "typescript-runner:$TAG"
fi

if [ "$FIRST_RUN" -eq 1 ]; then
    add_image "db-seeder:$TAG"
fi

# Запуск загрузки всех образов параллельно
while IFS= read -r img; do
    minikube -p "$MINIKUBE_PROFILE" image load "$img" &
    printf '%s|%s\n' "$!" "$img" >> "$jobs_file"
done < "$images_file"

# Ожидание завершения и вывод результата
all_ok=1

while IFS='|' read -r pid img; do
    if wait "$pid"; then
        printf 'Loaded %s\n' "$img"
    else
        printf 'Failed %s\n' "$img"
        all_ok=0
    fi
done < "$jobs_file"

printf 'All images have been loaded\n'
exit 0
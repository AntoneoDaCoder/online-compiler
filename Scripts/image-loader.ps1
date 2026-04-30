# load.ps1
param(
    [string]$TAG,
    [string]$MINIKUBE_PROFILE,
    [int]$COMPOSITE = 0,
    [int]$FIRST_RUN = 0
)

$images = @(
    "api-server:$TAG",
    "postgres:latest",
    "minio/minio:latest",
    "quay.io/keycloak/keycloak:latest"
)

if ($COMPOSITE -eq 1) {
    $images += "composite-runner:$TAG"
} else {
    $images += @(
        "csharp-runner:$TAG",
        "java-runner:$TAG",
        "nodejs-runner:$TAG",
        "kotlin-runner:$TAG",
        "typescript-runner:$TAG"
    )
}

if ($FIRST_RUN -eq 1) {
    $images += "db-seeder:$TAG"
}

# Запуск всех загрузок параллельно
$jobs = foreach ($img in $images) {
    Start-Job -Name $img -ScriptBlock {
        param($profile, $image)
        minikube -p $profile image load $image
    } -ArgumentList $MINIKUBE_PROFILE, $img
}

# Ожидание завершения
$jobs | Wait-Job | Out-Null

# Вывод результатов
$jobs | ForEach-Object {
    if ($_.State -eq 'Completed') {
        Write-Host "Loaded $($_.Name)" -ForegroundColor Green
    } else {
        Write-Host "Failed $($_.Name)" -ForegroundColor Red
    }
    Remove-Job $_
}

Write-Host "All images have been loaded" -ForegroundColor Green
@echo off
cd ..

if "%1"=="composite" (
    echo Building in COMPOSITE mode...
    docker buildx bake --file scripts/docker-bake.hcl composite --progress=plain
) else (
    echo Building in DEFAULT mode...
    docker buildx bake --file scripts/docker-bake.hcl default --progress=plain
)

if %errorlevel% neq 0 (
    echo [91mBUILD FAILED[0m
) else (
    echo [92mBUILD SUCCESS[0m
)

exit /b 0

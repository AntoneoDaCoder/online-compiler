@echo off
cd ..

docker buildx bake --file scripts/docker-bake.hcl --progress=plain
if %errorlevel% neq 0 (
    echo [91mBUILD FAILED[0m
) else (
    echo [92mBUILD SUCCESS[0m
)

exit /b 0
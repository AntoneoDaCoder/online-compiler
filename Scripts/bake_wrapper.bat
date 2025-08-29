@echo off

cd ..
docker buildx bake --file scripts/docker-bake.hcl --progress=plain 2>&1 | findstr /i "ERROR failed error"

if %errorlevel%==0 (
    echo [91mBUILD FAILED[0m
) else (
    echo [92mBUILD SUCCESS[0m
)

exit /b 0

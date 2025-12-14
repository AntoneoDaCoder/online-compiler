@start "" cmd /c "kubectl port-forward service/api-server 12345:8080"
@start "" cmd /c "kubectl -n postgresql port-forward service/postgres 5432:5432"
@start "" cmd /c "kubectl -n minio port-forward service/minio 9001:9001"

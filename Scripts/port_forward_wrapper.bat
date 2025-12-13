@start "" cmd /c "kubectl port-forward service/api-server 12345:8080"
@start "" cmd /c "kubectl port-forward service/postgres 5432:5432"
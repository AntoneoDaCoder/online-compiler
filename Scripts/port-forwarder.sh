#!/usr/bin/env bash
set -euo pipefail

is_windows_shell() {
  case "$(uname -s 2>/dev/null || true)" in
    MINGW*|MSYS*|CYGWIN*) return 0 ;;
    *) return 1 ;;
  esac
}

launch_linux() {
  local title="$1"
  local cmdline="$2"

  if command -v gnome-terminal >/dev/null 2>&1; then
    gnome-terminal --title="$title" -- bash -lc "$cmdline" >/dev/null 2>&1 &
  elif command -v konsole >/dev/null 2>&1; then
    konsole --new-tab -p tabtitle="$title" -e bash -lc "$cmdline" >/dev/null 2>&1 &
  elif command -v xfce4-terminal >/dev/null 2>&1; then
    xfce4-terminal --title="$title" -e "bash -lc $(printf '%q' "$cmdline")" >/dev/null 2>&1 &
  elif command -v kitty >/dev/null 2>&1; then
    kitty --title="$title" bash -lc "$cmdline" >/dev/null 2>&1 &
  elif command -v xterm >/dev/null 2>&1; then
    xterm -T "$title" -e bash -lc "$cmdline" >/dev/null 2>&1 &
  else
    echo "No GUI terminal found; falling back to nohup for $title" >&2
    nohup bash -lc "$cmdline" >/dev/null 2>&1 &
  fi
}

launch_windows() {
  local title="$1"
  local cmdline="$2"

  if command -v powershell.exe >/dev/null 2>&1; then
    PF_TITLE="$title" PF_CMD="$cmdline" powershell.exe -NoProfile -ExecutionPolicy Bypass -Command \
      "Start-Process -FilePath 'cmd.exe' -ArgumentList '/k', \$env:PF_CMD -WindowStyle Normal"
  else
    cmd.exe /c start "" cmd.exe /k "$cmdline"
  fi
}

launch_terminal() {
  local title="$1"
  local cmdline="$2"

  if is_windows_shell; then
    launch_windows "$title" "$cmdline"
  else
    launch_linux "$title" "$cmdline"
  fi
}

start_pf() {
  local ns="$1"
  local svc="$2"
  local local_port="$3"
  local remote_port="$4"
  local name="$5"
  local log="port-forward-${name}.log"

  : >"$log"

  local kubectl_cmd
  if [ -n "$ns" ]; then
    kubectl_cmd="kubectl -n \"$ns\" port-forward svc/$svc ${local_port}:${remote_port} > \"$log\" 2>&1"
  else
    kubectl_cmd="kubectl port-forward svc/$svc ${local_port}:${remote_port} > \"$log\" 2>&1"
  fi

  if is_windows_shell; then
    launch_windows "$name" "$kubectl_cmd"
  else
    launch_linux "$name" "$kubectl_cmd"
  fi
}

start_pf ""        api-server 12345 8080 api-server
start_pf postgresql postgres  5432  5432 postgres
start_pf minio     minio      9001  9001 minio-9001
start_pf minio     minio      9000  9000 minio-9000
start_pf keycloak  keycloak   8080  8080 keycloak

exit 0
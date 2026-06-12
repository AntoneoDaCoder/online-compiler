#!/usr/bin/env bash
set -euo pipefail

is_windows_shell() {
  case "$(uname -s 2>/dev/null || true)" in
    MINGW*|MSYS*|CYGWIN*) return 0 ;;
    *) return 1 ;;
  esac
}

PID_DIR="${PID_DIR:-.pf-pids}"
LOG_DIR="${LOG_DIR:-.pf-logs}"

mkdir -p "$PID_DIR" "$LOG_DIR"

pidfile_for() {
  printf '%s/%s.pid' "$PID_DIR" "$1"
}

logfile_for() {
  printf '%s/port-forward-%s.log' "$LOG_DIR" "$1"
}

is_running() {
  local pid="$1"

  if is_windows_shell; then
    tasklist /FI "PID eq $pid" /NH 2>/dev/null | grep -q "$pid"
  else
    kill -0 "$pid" 2>/dev/null
  fi
}

start_pf_linux() {
  local ns="$1"
  local svc="$2"
  local local_port="$3"
  local remote_port="$4"
  local name="$5"

  local pidfile log pid
  pidfile="$(pidfile_for "$name")"
  log="$(logfile_for "$name")"

  if [[ -f "$pidfile" ]]; then
    pid="$(cat "$pidfile" || true)"
    if [[ -n "${pid:-}" ]] && is_running "$pid"; then
      echo "$name already running (PID $pid)"
      return 0
    fi
    rm -f "$pidfile"
  fi

  : >"$log"

  if [[ -n "$ns" ]]; then
    nohup kubectl -n "$ns" port-forward "svc/$svc" "${local_port}:${remote_port}" >>"$log" 2>&1 < /dev/null &
  else
    nohup kubectl port-forward "svc/$svc" "${local_port}:${remote_port}" >>"$log" 2>&1 < /dev/null &
  fi

  pid=$!
  echo "$pid" >"$pidfile"
  echo "Started $name (PID $pid)"
}

start_pf_windows() {
  local ns="$1"
  local svc="$2"
  local local_port="$3"
  local remote_port="$4"
  local name="$5"

  local pidfile log
  pidfile="$(pidfile_for "$name")"
  log="$(logfile_for "$name")"

  if [[ -f "$pidfile" ]]; then
    local pid
    pid="$(cat "$pidfile" || true)"
    if [[ -n "${pid:-}" ]] && is_running "$pid"; then
      echo "$name already running (PID $pid)"
      return 0
    fi
    rm -f "$pidfile"
  fi

  : >"$log"
  : >"${log}.err"

  PF_NS="$ns" \
  PF_SVC="$svc" \
  PF_LOCAL="$local_port" \
  PF_REMOTE="$remote_port" \
  PF_PIDFILE="$pidfile" \
  PF_LOGFILE="$log" \
  PF_NAME="$name" \
  powershell.exe -NoProfile -ExecutionPolicy Bypass -Command '
    $ns = $env:PF_NS
    $svc = $env:PF_SVC
    $local = $env:PF_LOCAL
    $remote = $env:PF_REMOTE
    $pidfile = $env:PF_PIDFILE
    $logfile = $env:PF_LOGFILE
    $name = $env:PF_NAME

    $args = @()
    if ($ns) { $args += @("-n", $ns) }
    $args += @("port-forward", "svc/$svc", "$local`:$remote")

    $proc = Start-Process `
      -FilePath "kubectl.exe" `
      -ArgumentList $args `
      -PassThru `
      -WindowStyle Hidden `
      -RedirectStandardOutput $logfile `
      -RedirectStandardError "$logfile.err"

    Set-Content -Path $pidfile -Value $proc.Id -NoNewline

    Start-Sleep -Seconds 1
    if (-not (Get-Process -Id $proc.Id -ErrorAction SilentlyContinue)) {
      Write-Host "Failed to start $name (PID $($proc.Id))"
      if (Test-Path $logfile) { Get-Content $logfile -Tail 20 }
      if (Test-Path "$logfile.err") { Get-Content "$logfile.err" -Tail 20 }
      Remove-Item $pidfile -ErrorAction SilentlyContinue
      exit 1
    }

    Write-Host "Started $name (PID $($proc.Id))"
  '
}

start_pf() {
  local ns="$1"
  local svc="$2"
  local local_port="$3"
  local remote_port="$4"
  local name="$5"

  if is_windows_shell; then
    start_pf_windows "$ns" "$svc" "$local_port" "$remote_port" "$name"
  else
    start_pf_linux "$ns" "$svc" "$local_port" "$remote_port" "$name"
  fi
}

stop_pf() {
  local name="$1"
  local pidfile pid

  pidfile="$(pidfile_for "$name")"

  if [[ ! -f "$pidfile" ]]; then
    echo "Not found: $name"
    return 0
  fi

  pid="$(cat "$pidfile" || true)"
  if [[ -z "${pid:-}" ]]; then
    rm -f "$pidfile"
    echo "Stale pidfile removed: $name"
    return 0
  fi

  if is_running "$pid"; then
    if is_windows_shell; then
      taskkill /PID "$pid" /T /F >/dev/null 2>&1 || true
    else
      kill "$pid" 2>/dev/null || true
      for _ in {1..10}; do
        if ! is_running "$pid"; then
          break
        fi
        sleep 0.2
      done
      if is_running "$pid"; then
        kill -9 "$pid" 2>/dev/null || true
      fi
    fi
    echo "Stopped $name (PID $pid)"
  else
    echo "$name was not running (stale PID $pid)"
  fi

  rm -f "$pidfile"
}

status_pf() {
  local name="$1"
  local pidfile pid

  pidfile="$(pidfile_for "$name")"
  if [[ -f "$pidfile" ]]; then
    pid="$(cat "$pidfile" || true)"
    if [[ -n "${pid:-}" ]] && is_running "$pid"; then
      echo "$name: running (PID $pid)"
    else
      echo "$name: stale pidfile"
    fi
  else
    echo "$name: not running"
  fi
}

start_all() {
  start_pf ""         api-server 12345 8080 api-server
  start_pf postgresql postgres   5432  5432 postgres
  start_pf minio      minio      9001  9001 minio-9001
  start_pf minio      minio      9000  9000 minio-9000
  start_pf keycloak   keycloak   8080  8080 keycloak
}

stop_all() {
  stop_pf api-server
  stop_pf postgres
  stop_pf minio-9001
  stop_pf minio-9000
  stop_pf keycloak
}

status_all() {
  status_pf api-server
  status_pf postgres
  status_pf minio-9001
  status_pf minio-9000
  status_pf keycloak
}

case "${1:-start}" in
  start)  start_all ;;
  stop)   stop_all ;;
  status) status_all ;;
  *)
    echo "Usage: $0 {start|stop|status}" >&2
    exit 1
    ;;
esac
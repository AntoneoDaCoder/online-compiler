$ErrorActionPreference = 'Stop'

$esc = [char]27
$Y = "${esc}[93m"
$G = "${esc}[92m"
$R = "${esc}[91m"
$N = "${esc}[0m"

# --- config ---------------------------------------------------------------

$Images = @(
  'api-server:local',
  'csharp-runner:local',
  'java-runner:local',
  'postgresql-runner:local',
  'mssql-runner:local',
  #'swift-runner:local',
  'nodejs-runner:local',
  'kotlin-runner:local',
  'typescript-runner:local'
)

function Get-ImageId($tag) {
  try { docker image inspect -f '{{.Id}}' $tag 2>$null } catch { $null }
}

$RunnerMap = @{
  'csharp-runner:local'     = @{ ns='csharp-runners-namespace';     dep='csharp-runners-deployment' }
  'java-runner:local'       = @{ ns='java-runners-namespace';       dep='java-runners-deployment' }
  'postgresql-runner:local' = @{ ns='postgresql-runners-namespace'; dep='postgresql-runners-deployment' }
  'mssql-runner:local'      = @{ ns='mssql-runners-namespace';      dep='mssql-runners-deployment' }
  'nodejs-runner:local'     = @{ ns='nodejs-runners-namespace';     dep='nodejs-runners-deployment' }
  'kotlin-runner:local'     = @{ ns='kotlin-runners-namespace';     dep='kotlin-runners-deployment' }
  'typescript-runner:local' = @{ ns='typescript-runners-namespace'; dep='typescript-runners-deployment' }
  #'swift-runner:local'     = @{ ns='swift-runners-namespace';      dep='swift-runners-deployment' }
}

# --- helpers --------------------------------------------------------------

function Mk-SSH([string]$cmd) {
  # always run through bash -lc to support pipes/filters
  & minikube ssh -- bash -lc $cmd
}

function Remove-OldImageByTag-InMinikube([string]$tag) {
Write-Host "[rebuild] Cleaning containers referencing $img in Minikube..."
$containers = & minikube ssh -- docker ps -a -q --filter "ancestor=$img" 2>$null

if (-not [string]::IsNullOrWhiteSpace($containers)) {
    foreach ($c in $containers -split "`n") {
        if (-not [string]::IsNullOrWhiteSpace($c)) {
            Write-Host "  Removing container $c..."
            & minikube ssh -- docker rm -f $c 2>$null
        }
    }
} else {
    Write-Host "  No containers found for $img"
}

}

function Restart-PortForward {
  Write-Host "${Y}[rebuild] Restarting port-forward to API...${N}"

  # kill ALL port-forward processes for api-server
  Get-CimInstance Win32_Process -Filter "name = 'kubectl.exe'" |
    Where-Object { $_.CommandLine -like "*port-forward*api-server*" } |
    ForEach-Object {
      Write-Host "Killing old port-forward process (PID=$($_.ProcessId))"
      Stop-Process -Id $_.ProcessId -Force
    }

  Start-Sleep -Seconds 2

  # start new one in separate cmd window, detached from make
  Start-Process cmd.exe -ArgumentList '/k title API-PortForward && kubectl port-forward service/api-server 12345:8080' -WindowStyle Normal
  Write-Host "${G}[rebuild] New port-forward started (12345 -> 8080)${N}"
}

# --- capture BEFORE -------------------------------------------------------

Write-Host "${Y}[rebuild] Capturing image IDs before build...${N}"
$Before = @{}
foreach ($img in $Images) {
  $Id = Get-ImageId $img
  $Before[$img] = $Id
  Write-Host "Tag: {$img}, SHA: {$Id}"
}

# --- build via bake -------------------------------------------------------

Write-Host "${Y}[rebuild] Running bake (parallel)...${N}"
& "$PSScriptRoot\bake_wrapper.bat"
if ($LASTEXITCODE -ne 0) {
  Write-Host "${R}[rebuild] bake returned non-zero exit code ($LASTEXITCODE). Continuing...${N}"
}

# --- capture AFTER --------------------------------------------------------

Write-Host "${Y}[rebuild] Capturing image IDs after build...${N}"
$After = @{}
foreach ($img in $Images) {
  $Id = Get-ImageId $img
  $After[$img] = $Id
  Write-Host "Tag: {$img}, SHA: {$Id}"
}

# --- detect changes -------------------------------------------------------

$Changed = @()
foreach ($img in $Images) {
  $beforeId = $Before[$img]
  $afterId  = $After[$img]
  if ([string]::IsNullOrEmpty($afterId)) { continue }  # build failed for this tag
  if ($beforeId -ne $afterId) { $Changed += $img }
}

if ($Changed.Count -eq 0) {
  Write-Host "${G}[rebuild] No image changes detected. Nothing to update in Minikube.${N}"
  # show current deployments anyway
  Write-Host "${Y}[rebuild] Current deployment statuses:${N}"
  kubectl get deploy
  Write-Host "${G}[rebuild] Done.${N}"
  exit 0
}

# --- update changed images in Minikube -----------------------------------

Write-Host "${Y}[rebuild] Updating changed images in Minikube...${N}"

$NeedApiRestart = $Changed -contains 'api-server:local'

foreach ($img in $Changed) {
  $oldId = $Before[$img]
  $newId = $After[$img]
  Write-Host "$img"

  # 1) scale down if runner has k8s deployment mapping
  $hasMap = $RunnerMap.ContainsKey($img)
  if ($hasMap) {
    $ns  = $RunnerMap[$img].ns
    $dep = $RunnerMap[$img].dep
    Write-Host "[rebuild] Scaling down deployment for $img..."
    kubectl -n $ns scale deployment $dep --replicas=0
    kubectl -n $ns rollout status deployment $dep --timeout=60s
  } elseif ($img -eq 'api-server:local') {
    Write-Host "[rebuild] Scaling down API deployment..."
    kubectl scale deployment api-server --replicas=0
    kubectl rollout status deployment api-server --timeout=60s
  }

  # 2) remove old image(s) for this tag inside Minikube (also prunes stopped containers)
  Remove-OldImageByTag-InMinikube $img

  # 3) load new image
  Write-Host "[rebuild] Loading new image $img into Minikube..."
  & minikube -p minikube image load $img
  if ($LASTEXITCODE -ne 0) {
    Write-Host "${R}[rebuild] Failed to load $img into Minikube (exit $LASTEXITCODE)${N}"
    exit $LASTEXITCODE
  }

  # 4) scale up back
  if ($hasMap) {
    $ns  = $RunnerMap[$img].ns
    $dep = $RunnerMap[$img].dep
    Write-Host "[rebuild] Scaling up deployment for $img..."
    kubectl -n $ns scale deployment $dep --replicas=1
    kubectl -n $ns rollout status deployment $dep --timeout=180s
  } elseif ($img -eq 'api-server:local') {
    Write-Host "[rebuild] Scaling up API deployment..."
    kubectl scale deployment api-server --replicas=1
    kubectl rollout status deployment api-server --timeout=180s
  }
}

# restart port-forward ONLY if api-server changed
if ($NeedApiRestart) { Restart-PortForward }

Write-Host "${G}[rebuild] Done.${N}"
exit 0

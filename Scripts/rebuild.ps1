$ErrorActionPreference = 'Stop'

$esc = [char]27
$Y = "${esc}[93m"
$G = "${esc}[92m"
$R = "${esc}[91m"
$N = "${esc}[0m"


# Our tag list (the one that bake builds)
$Images = @(
  'api-server:local',
  'csharp-runner:local',
  'java-runner:local',
  'postgresql-runner:local',
  #'swift-runner:local',
  'nodejs-runner:local',
  'kotlin-runner:local',
  'typescript-runner:local'
)

function Get-ImageId($tag) {
  try { docker image inspect -f '{{.Id}}' $tag 2>$null } catch { $null }
}

# map for comparison: tag -> deployment name && namespace
$RunnerMap = @{
  'csharp-runner:local'     = @{ ns='csharp-runners-namespace';     dep='csharp-runners-deployment' }
  'java-runner:local'       = @{ ns='java-runners-namespace';       dep='java-runners-deployment' }
  'postgresql-runner:local' = @{ ns='postgresql-runners-namespace'; dep='postgresql-runners-deployment' }
  'nodejs-runner:local'     = @{ ns='nodejs-runners-namespace';     dep='nodejs-runners-deployment' }
  'kotlin-runner:local'     = @{ ns='kotlin-runners-namespace';     dep='kotlin-runners-deployment' }
  'typescript-runner:local' = @{ ns='typescript-runners-namespace'; dep='typescript-runners-deployment' }
  #'swift-runner:local' = @{ ns='swift-runners-namespace'; dep='swift-runners-deployment'}
}

Write-Host "${Y}[rebuild] Capturing image IDs before build...${N}"
$Before = @{}
foreach ($img in $Images)
{
    $Id = Get-ImageId $img 
    $Before[$img] = $Id
    Write-Host "Tag: {$img}, SHA: {$Id}"
}

# building images via bake
Write-Host "${Y}[rebuild] Running bake (parallel)...${N}"
& "$PSScriptRoot\bake_wrapper.bat"
if ($LASTEXITCODE -ne 0) {
  Write-Host "${R}[rebuild] bake returned non-zero exit code ($LASTEXITCODE). Continuing to check built images...${N}"
}

Write-Host "${Y}[rebuild] Capturing image IDs after build...${N}"
$After = @{}
foreach ($img in $Images)
{
    $Id = Get-ImageId $img 
    $After[$img] = $Id
    Write-Host "Tag: {$img}, SHA: {$Id}"
}

# Trying to find changed images
$Changed = @()
foreach ($img in $Images) {
  $beforeId = $Before[$img]
  $afterId  = $After[$img]
  if ([string]::IsNullOrEmpty($afterId)) { continue }           # image build finished with an error
  if ($beforeId -ne $afterId) { $Changed += $img }
}

if ($Changed.Count -eq 0) {
  Write-Host "${G}[rebuild] No image changes detected. Nothing to load into Minikube.${N}"
} else {
  Write-Host "${Y}[rebuild] Loading changed images into Minikube...${N}"
  foreach ($img in $Changed) {
    Write-Host "  → $img"
    & minikube -p minikube image load $img
    if ($LASTEXITCODE -ne 0) {
      Write-Host "${R}[rebuild] Failed to load $img into Minikube (exit $LASTEXITCODE)${N}"
      exit $LASTEXITCODE
    }
  }
  Write-Host "${G}[rebuild] Images loaded into Minikube.${N}"
}


function Restart-PortForward {
    Write-Host "${Y}[rebuild] Restarting port-forward to API...${N}"

    # kill old port-forward
    Get-Process kubectl -ErrorAction SilentlyContinue | Where-Object {
        $_.Path -like "*kubectl*" -and $_.StartInfo.Arguments -like "*port-forward*api-server*"
    } | ForEach-Object {
        Write-Host "Killing old port-forward process (PID=$($_.Id))"
        Stop-Process -Id $_.Id -Force
    }

    Start-Sleep -Seconds 2

    # new port-forward
    Start-Process -NoNewWindow cmd.exe "/c kubectl port-forward service/api-server 12345:8080"
    Write-Host "${G}[rebuild] New port-forward started (12345 -> 8080)${N}"
}


# Restarting API server and changed drivers
$NeedApiRestart = $Changed -contains 'api-server:local'
$ChangedRunners = $Changed | Where-Object { $_ -ne 'api-server:local' }

if ($NeedApiRestart) {
  Write-Host "${Y}[rebuild] Restarting API deployment...${N}"
  kubectl scale deployment api-server --replicas=0
  kubectl rollout status deployment api-server --timeout=60s
  kubectl scale deployment api-server --replicas=1
  kubectl rollout status deployment api-server --timeout=180s
  Restart-PortForward
}

foreach ($img in $ChangedRunners) {
  if (-not $RunnerMap.ContainsKey($img)) { continue }
  $ns  = $RunnerMap[$img].ns
  $dep = $RunnerMap[$img].dep
  Write-Host "${Y}[rebuild] Restarting runner deployment ($img) in ns '$ns'...${N}"
  kubectl -n $ns scale deployment $dep --replicas=0
  kubectl -n $ns rollout status deployment $dep --timeout=60s
  kubectl -n $ns scale deployment $dep --replicas=1
  kubectl -n $ns rollout status deployment $dep --timeout=180s
}

# if nothing has changed print api status anyway
if (-not $NeedApiRestart -and $ChangedRunners.Count -eq 0) {
  Write-Host "${Y}[rebuild] Nothing changed. Current deployment statuses:${N}"
  kubectl get deploy
}

Write-Host "${G}[rebuild] Done.${N}"
exit 0

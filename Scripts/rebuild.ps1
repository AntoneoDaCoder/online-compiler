param(
    [ValidateSet("default","composite")]
    [string]$Mode = "default"
)

$ErrorActionPreference = 'Stop'

$esc = [char]27
$Y = "${esc}[93m"
$G = "${esc}[92m"
$R = "${esc}[91m"
$N = "${esc}[0m"

Write-Host "${Y}[rebuild] Running in mode: $Mode ${N}"

# --- config --------------------------------------------------------------

if ($Mode -eq "composite") {
    $Images = @(
        'api-server:local',
        'composite-runner:local'
    )

    $RunnerMap = @{
        'composite-runner:local' = @{ ns='composite-runners-namespace'; dep='composite-runners-deployment' }
    }
}
else {
    $Images = @(
        'api-server:local',
        'csharp-runner:local',
        'java-runner:local',
        'db-seeder:local',
        'nodejs-runner:local',
        'kotlin-runner:local',
        'typescript-runner:local'
    )

    $RunnerMap = @{
        'csharp-runner:local'     = @{ ns='csharp-runners-namespace';     dep='csharp-runners-deployment' }
        'java-runner:local'       = @{ ns='java-runners-namespace';       dep='java-runners-deployment' }
        'postgresql-runner:local' = @{ ns='postgresql-runners-namespace'; dep='postgresql-runners-deployment' }
        'nodejs-runner:local'     = @{ ns='nodejs-runners-namespace';     dep='nodejs-runners-deployment' }
        'kotlin-runner:local'     = @{ ns='kotlin-runners-namespace';     dep='kotlin-runners-deployment' }
        'typescript-runner:local' = @{ ns='typescript-runners-namespace'; dep='typescript-runners-deployment' }
    }
}

function Get-ImageId($tag) {
  try { docker image inspect -f '{{.Id}}' $tag 2>$null } catch { $null }
}

function Mk-SSH([string]$cmd) {
  & minikube ssh -- bash -lc $cmd
}

function Remove-OldImageByTag-InMinikube([string]$tag, [string]$oldId) {
  <#
    Удаляет контейнеры, основанные на теге, затем пытается удалить образ по id.
    Возвращает $true если удалось удалить образ, иначе $false.
  #>
  Write-Host "[rebuild] Cleaning containers referencing $tag in Minikube..."
  $containersRaw = & minikube ssh -- docker ps -a -q --filter "ancestor=$tag" 2>$null
  if ($LASTEXITCODE -ne 0) { $containersRaw = "" }

  if (-not [string]::IsNullOrWhiteSpace($containersRaw)) {
      $containers = $containersRaw -split "`n" | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne "" }
      foreach ($c in $containers) {
          Write-Host "  Removing container $c..."
          & minikube ssh -- docker rm -f $c 2>$null
      }
  } else {
      Write-Host "  No containers found for $tag"
  }

  if (-not [string]::IsNullOrWhiteSpace($oldId)) {
      Write-Host "[rebuild] Attempting to remove old image id $oldId from Minikube..."
      # попробуем удалить по id (без -f сначала), если не выйдет - попытаемся с -f
      $r1 = & minikube ssh -- docker rmi $oldId 2>$null
      $rc = $LASTEXITCODE
      if ($rc -ne 0) {
          # попробуем форсированно (но это может провалиться, если контейнер всё ещё использует образ)
          $r2 = & minikube ssh -- docker rmi -f $oldId 2>$null
          $rc2 = $LASTEXITCODE
          if ($rc2 -ne 0) {
              Write-Host "${Y}[rebuild] Warning: could not remove old image id $oldId (rc=$rc2)${N}"
              return $false
          } else {
              Write-Host "${G}[rebuild] Old image removed.${N}"
              return $true
          }
      } else {
          Write-Host "${G}[rebuild] Old image removed.${N}"
          return $true
      }
  } else {
      Write-Host "  No old image id provided, skipping image delete."
      return $true
  }
}

function Restart-PortForward {
  Write-Host "${Y}[rebuild] Restarting port-forward to API...${N}"

  # kill existing port-forward processes started via kubectl
  Get-CimInstance Win32_Process -Filter "name = 'kubectl.exe'" |
    Where-Object { $_.CommandLine -like "*port-forward*api-server*" } |
    ForEach-Object {
      Write-Host "Killing old port-forward process (PID=$($_.ProcessId))"
      try { Stop-Process -Id $_.ProcessId -Force } catch { }
    }

  Start-Sleep -Seconds 2
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

Write-Host "${Y}[rebuild] Running bake (parallel, mode=$Mode)...${N}"
& "$PSScriptRoot\bake_wrapper.bat" $Mode
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
  if ([string]::IsNullOrEmpty($afterId)) { continue }           # image didn't build
  if ($beforeId -ne $afterId) { $Changed += $img }
}

if ($Changed.Count -eq 0) {
  Write-Host "${G}[rebuild] No image changes detected. Nothing to update in Minikube.${N}"
  Write-Host "${Y}[rebuild] Current deployment statuses:${N}"
  kubectl get deploy
  Write-Host "${G}[rebuild] Done.${N}"
  exit 0
}

# --- update changed images in Minikube -----------------------------------

Write-Host "${Y}[rebuild] Updating changed images in Minikube...${N}"

$NeedApiRestart = $Changed -contains 'api-server:local'

# If API changed, first DELETE all runner deployments so API will recreate them on start.
if ($NeedApiRestart) {
    Write-Host "${Y}[rebuild] api-server changed -> deleting all runner deployments so API will recreate them...${N}"
    foreach ($k in $RunnerMap.Keys) {
        $ns = $RunnerMap[$k].ns
        $dep = $RunnerMap[$k].dep
        Write-Host "[rebuild] Deleting deployment $dep in ns $ns (ignore-not-found)..."
        kubectl -n $ns delete deployment $dep --ignore-not-found
    }
    Write-Host "${Y}[rebuild] All runner deployments removed (API is expected to recreate them on startup).${N}"
}

foreach ($img in $Changed) {
  Write-Host "$img"

  $hasMap = $RunnerMap.ContainsKey($img)

  # If API changed: we do not need to scale down each runner (we already deleted them above).
  if (-not $NeedApiRestart) {
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
  } else {
      # If NeedApiRestart - ensure API deployment is scaled down (we will update API image next)
      if ($img -eq 'api-server:local') {
          Write-Host "[rebuild] Scaling down API deployment..."
          kubectl scale deployment api-server --replicas=0
          kubectl rollout status deployment api-server --timeout=60s
      }
      # runners were deleted earlier, so skip scaling down them
  }

  $oldId = $Before[$img]
  $newId = $After[$img]

  # Remove old image (attempt)
  if ($oldId -and $oldId -ne $newId) {
      $removed = Remove-OldImageByTag-InMinikube $img $oldId
      if (-not $removed) {
          Write-Host "${Y}[rebuild] Warning: old image could not be removed. Will still try to load new image.${N}"
      }
  } else {
      Write-Host "  No old image to remove (or identical)."
  }

  # Load new image
  Write-Host "[rebuild] Loading new image $img into Minikube..."
  & minikube -p minikube image load $img
  if ($LASTEXITCODE -ne 0) {
    Write-Host "${R}[rebuild] Failed to load $img into Minikube (exit $LASTEXITCODE)${N}"
    exit $LASTEXITCODE
  }

  # If API changed: scale it up only when its image processed
  if ($img -eq 'api-server:local') {
      Write-Host "[rebuild] Scaling up API deployment..."
      kubectl scale deployment api-server --replicas=1
      kubectl rollout status deployment api-server --timeout=180s
  } else {
      # For runners: if we deleted them earlier (NeedApiRestart) we must NOT scale them here.
      if (-not $NeedApiRestart -and $hasMap) {
        $ns  = $RunnerMap[$img].ns
        $dep = $RunnerMap[$img].dep
        Write-Host "[rebuild] Scaling up deployment for $img..."
        kubectl -n $ns scale deployment $dep --replicas=1
        kubectl -n $ns rollout status deployment $dep --timeout=180s
      } else {
        Write-Host "  Skipping runner scale-up because API restart mode will let API recreate runners."
      }
  }
}

if ($NeedApiRestart) { Restart-PortForward }

Write-Host "${G}[rebuild] Done.${N}"
exit 0

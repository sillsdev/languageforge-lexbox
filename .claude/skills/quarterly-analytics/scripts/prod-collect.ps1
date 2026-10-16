#requires -Version 7
<#
.SYNOPSIS
  Collects the prod-only quarterly analytics. Run it yourself: it reads from the aws-prod cluster.
.EXAMPLE
  pwsh .claude/skills/quarterly-analytics/scripts/prod-collect.ps1 -Quarter FY27Q1
.NOTES
  db: port-forwards the prod DB to localhost, runs prod-queries.sql read-only (psql asks for the
      password), then stops the port-forward.
  hg: pipes hg-active-repos.sh into the hg pod (sequential, low priority, one scan at a time).
      The lock lives in the pod, so don't run this step while hg is being redeployed (two pods).
  Both bring back counts, which this prints at the end; the scan also names any repo hg fails on.
  Nothing sensitive is written to disk: kubectl's port-forward log goes to a temp file that's
  deleted afterwards.
#>
param(
  [Parameter(Mandatory)][ValidatePattern('^FY\d{2}Q[1-4]$', Options = 'None')][string]$Quarter,
  [ValidateSet('all', 'db', 'hg')][string]$Step = 'all',
  [string]$Context = 'aws-prod',
  [string]$Namespace = 'languagedepot',
  [int]$LocalPort = 5435,
  [string]$Psql
)
$ErrorActionPreference = 'Stop'

$fy = 2000 + [int]$Quarter.Substring(2, 2)
$q = [int]$Quarter.Substring(5, 1)
$startDate = [datetime]::new($(if ($q -eq 1) { $fy - 1 } else { $fy }), @(10, 1, 4, 7)[$q - 1], 1)
$start = $startDate.ToString('yyyy-MM-dd')
$end = $startDate.AddMonths(3).ToString('yyyy-MM-dd')
$lastDay = $startDate.AddMonths(3).AddDays(-1).ToString('yyyy-MM-dd')
$mtimeCutoff = $startDate.AddDays(-30).ToString('yyyy-MM-dd')
Write-Host "$Quarter = $start .. $lastDay"
$results = [ordered]@{ quarter = $Quarter }

function Test-LocalPort([int]$port) {
  $client = [Net.Sockets.TcpClient]::new()
  try { $client.ConnectAsync('127.0.0.1', $port).Wait(300) -and $client.Connected } catch { $false } finally { $client.Dispose() }
}

if ($Step -in 'all', 'db') {
  if (-not $Psql) {
    $Psql = Get-Command psql -ErrorAction SilentlyContinue | Select-Object -First 1 -ExpandProperty Source
    if (-not $Psql -and $IsWindows) {
      $Psql = Get-ChildItem 'C:\Program Files\PostgreSQL\*\bin\psql.exe' -ErrorAction SilentlyContinue |
        Sort-Object { [int]$_.Directory.Parent.Name } | Select-Object -Last 1 -ExpandProperty FullName
    }
    if (-not $Psql) { throw 'psql not found; pass -Psql <path>.' }
  }
  if (Test-LocalPort $LocalPort) { throw "Port $LocalPort is already in use; stop that port-forward or pass -LocalPort." }

  # The forward logs every connection to stdout, which would land in the middle of psql's password prompt.
  $pfLog = [IO.Path]::GetTempFileName()
  $pf = Start-Process kubectl -PassThru -NoNewWindow -RedirectStandardOutput $pfLog `
    -ArgumentList 'port-forward', 'service/db', "${LocalPort}:5432", '-n', $Namespace, '--context', $Context
  try {
    $deadline = (Get-Date).AddSeconds(30)
    while (-not (Test-LocalPort $LocalPort)) {
      if ($pf.HasExited -or (Get-Date) -gt $deadline) { throw "Port-forward didn't come up; kubectl's error, if any, is above." }
      Start-Sleep -Milliseconds 500
    }
    $csv = & $Psql -X -q -h 127.0.0.1 -p $LocalPort -U postgres -d lexbox `
      -v "start=$start" -v "end=$end" -f (Join-Path $PSScriptRoot 'prod-queries.sql')
    if ($LASTEXITCODE -ne 0) { throw "psql failed ($LASTEXITCODE)." }
  } finally {
    # kubectl may be a shim (e.g. Chocolatey) whose child holds the forward, so kill the whole tree.
    if (-not $pf.HasExited) { $pf.Kill($true) }
    Start-Sleep -Milliseconds 500
    Remove-Item $pfLog -ErrorAction SilentlyContinue
    if (Test-LocalPort $LocalPort) { Write-Warning "Something still listens on port $LocalPort; check for a leftover kubectl port-forward." }
  }
  foreach ($column in ($csv | ConvertFrom-Csv).PSObject.Properties) { $results[$column.Name] = [int]$column.Value }
}

if ($Step -in 'all', 'hg') {
  $script = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'hg-active-repos.sh')) -replace "`r", ''
  $counts = $script | kubectl exec -i -n $Namespace --context $Context deploy/hg -c hgweb -- sh -s -- $start $lastDay $mtimeCutoff
  if ($LASTEXITCODE -ne 0) {
    throw "hg scan failed ($LASTEXITCODE). If the connection dropped, the scan may still be running in the pod: a re-run refuses to start until it has finished."
  }
  foreach ($pair in (@($counts | Where-Object { $_ })[-1] -split ' ')) {
    $k, $v = $pair -split '='
    $results[$k] = [int]$v
  }
  if ($results.hg_failures) { Write-Warning "hg log failed on $($results.hg_failures) repos (named above); those aren't counted." }
}

[pscustomobject]$results | Format-List

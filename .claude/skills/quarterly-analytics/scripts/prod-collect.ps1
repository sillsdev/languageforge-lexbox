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
  Output holds project codes, so it must not be inside any git work tree:
  $env:LEXBOX_ANALYTICS_DIR or ~/lexbox-analytics, then /<Quarter>.
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

$baseDir = if ($env:LEXBOX_ANALYTICS_DIR) { $env:LEXBOX_ANALYTICS_DIR } else { Join-Path $HOME 'lexbox-analytics' }
$outDir = [IO.Path]::GetFullPath((Join-Path $baseDir $Quarter), $PWD.Path)
$created = -not (Test-Path $outDir)
New-Item -ItemType Directory -Force $outDir | Out-Null
$inside = & { $PSNativeCommandUseErrorActionPreference = $false; git -C $outDir rev-parse --is-inside-work-tree 2>$null }
if ($inside -eq 'true') {
  if ($created) { Remove-Item $outDir }
  throw "Output dir $outDir is inside a git work tree; it would hold project codes. Set LEXBOX_ANALYTICS_DIR elsewhere."
}
Write-Host "$Quarter = $start .. $lastDay, output: $outDir"

$filter = 'test|train|demo|dev|copy'
$summaryPath = Join-Path $outDir 'prod-summary.json'
$summary = if (Test-Path $summaryPath) { Get-Content $summaryPath -Raw | ConvertFrom-Json -AsHashtable } else { @{} }
$summary.quarter = $Quarter; $summary.start = $start; $summary.end = $end
function Save-Summary { $summary | ConvertTo-Json -Depth 4 | Set-Content $summaryPath }

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

  $pf = Start-Process kubectl -PassThru -NoNewWindow `
    -ArgumentList 'port-forward', 'service/db', "${LocalPort}:5432", '-n', $Namespace, '--context', $Context `
    -RedirectStandardOutput (Join-Path $outDir 'port-forward.log') -RedirectStandardError (Join-Path $outDir 'port-forward.err.log')
  try {
    $deadline = (Get-Date).AddSeconds(30)
    while (-not (Test-LocalPort $LocalPort)) {
      if ($pf.HasExited -or (Get-Date) -gt $deadline) { throw "Port-forward didn't come up; see port-forward.err.log." }
      Start-Sleep -Milliseconds 500
    }
    # psql treats backslashes in quoted meta-command arguments as escapes, so hand it forward slashes.
    $fwlOut = (Join-Path $outDir 'fwl-projects.csv') -replace '\\', '/'
    $lexOut = (Join-Path $outDir 'lexbox-created.csv') -replace '\\', '/'
    & $Psql -X -h 127.0.0.1 -p $LocalPort -U postgres -d lexbox `
      -v "start=$start" -v "end=$end" -v "fwl_out=$fwlOut" -v "lexbox_out=$lexOut" `
      -f (Join-Path $PSScriptRoot 'prod-queries.sql')
    if ($LASTEXITCODE -ne 0) { throw "psql failed ($LASTEXITCODE)." }
  } finally {
    # kubectl may be a shim (e.g. Chocolatey) whose child holds the forward, so kill the whole tree.
    if (-not $pf.HasExited) { $pf.Kill($true) }
    Start-Sleep -Milliseconds 500
    if (Test-LocalPort $LocalPort) { Write-Warning "Something still listens on port $LocalPort; check for a leftover kubectl port-forward." }
  }

  $fwl = Import-Csv $fwlOut
  $summary.fwl = @{
    activeProjects = $fwl.Count
    newProjects = @($fwl | Where-Object new_in_quarter -eq 't').Count
    languagesImpacted = @($fwl | Where-Object { $_.code -notmatch $filter }).Count
  }
  $summary.lexboxCreatedProjects = [int](Import-Csv $lexOut).created_in_quarter
  Save-Summary
}

if ($Step -in 'all', 'hg') {
  $hgOut = Join-Path $outDir 'lexbox-active-repos.txt'
  $script = [IO.File]::ReadAllText((Join-Path $PSScriptRoot 'hg-active-repos.sh')) -replace "`r", ''
  $script | kubectl exec -i -n $Namespace --context $Context deploy/hg -c hgweb -- sh -s -- $start $lastDay $mtimeCutoff > $hgOut
  if ($LASTEXITCODE -ne 0) {
    throw "hg scan failed ($LASTEXITCODE); partial output is in $hgOut. If the connection dropped, the scan may still be running in the pod: a re-run refuses to start until it has finished."
  }
  $lines = @(Get-Content $hgOut | Where-Object { $_ })
  $repos = @($lines | Where-Object { -not $_.StartsWith('#') })
  $stats = @{}
  foreach ($pair in ($lines | Where-Object { $_.StartsWith('#') } | Select-Object -Last 1) -split ' ' | Where-Object { $_ -match '=' }) {
    $k, $v = $pair -split '='; $stats[$k] = [int]$v
  }
  $summary.lexbox = @{
    activeProjects = $repos.Count
    languagesImpacted = @($repos | Where-Object { $_ -notmatch $filter }).Count
    reposScanned = $stats.repos
    hgFailures = $stats.failed
  }
  Save-Summary
  if ($stats.failed) { Write-Warning "hg log failed on $($stats.failed) repos (listed above); those aren't counted." }
}

Get-Content $summaryPath

#Requires -Version 7
<#
.SYNOPSIS
  Interactive harness for testing the FwLite Windows auto-update flow end-to-end, locally.

.DESCRIPTION
  Builds two MSIX bundles of FwLite MAUI that share ONE test package identity
  (FwLiteDesktopTest, distinct from the production FwLiteDesktop) but different versions,
  so an in-place update can replace the lower with the higher. Runs a fake lexbox update
  API (UpdateTestServer) on localhost that the app checks, and lets you install / launch /
  uninstall and pick which version the server offers.

  See README.md for the full walkthrough. Menu-driven; just run it.

.NOTES
  Windows only. Trusting the self-signed signing cert needs admin once (per machine, shared
  across all worktrees via the Windows cert store).
#>
[CmdletBinding()]
param(
    [int]$Port = 5199,
    [switch]$Force  # rebuild bundles even if they already exist
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

# ---------------------------------------------------------------------------- paths / constants
$RepoRoot     = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path
$MauiCsproj   = Join-Path $RepoRoot 'backend\FwLite\FwLiteMaui\FwLiteMaui.csproj'
$ViewerDir    = Join-Path $RepoRoot 'frontend\viewer'
$ServerProj   = Join-Path $PSScriptRoot 'UpdateTestServer\UpdateTestServer.csproj'
$ServerExe    = Join-Path $PSScriptRoot 'UpdateTestServer\bin\Release\net10.0\UpdateTestServer.exe'
# Build output under the repo's gitignored artifacts/ (per-worktree on purpose).
$ArtifactsDir = Join-Path $RepoRoot 'backend\FwLite\artifacts\harness'
# Cross-worktree state (cert stays in the Windows cert store; this just holds the exported cer + server pid).
$HarnessHome  = Join-Path $env:LOCALAPPDATA 'FwLite\update-test-harness'
$StateFile    = Join-Path $HarnessHome 'state.json'
$CerFile      = Join-Path $HarnessHome 'FwLiteTestCert.cer'
$BuildsJson   = Join-Path $ArtifactsDir 'builds.json'

$PackageIdentity = 'FwLiteDesktopTest'   # distinct from prod 'FwLiteDesktop' so the real install is untouched
$CertSubject     = 'CN=FwLiteTestCert'
$AppId           = 'App'                  # <Application Id="App"> in the manifest
$Framework       = 'net10.0-windows10.0.19041.0'
$ServerOutLog    = Join-Path $HarnessHome 'server.out.log'
$ServerErrLog    = Join-Path $HarnessHome 'server.err.log'
function Get-UpdateUrl { "http://localhost:$Port/api/fwlite-release/should-update" }

# Two builds: same identity, different versions. Quad = MSIX package/bundle version; Info = shown in the app UI.
$Versions = [ordered]@{
    v1 = @{ Semver = '0.0.1'; Quad = '0.0.1.0'; Info = 'v0.0.1-test'; Bundle = 'FwLiteDesktopTest-v1.msixbundle' }
    v2 = @{ Semver = '0.0.2'; Quad = '0.0.2.0'; Info = 'v0.0.2-test'; Bundle = 'FwLiteDesktopTest-v2.msixbundle' }
}

# ---------------------------------------------------------------------------- small helpers
function Info($m)  { Write-Host $m -ForegroundColor Cyan }
function Ok($m)    { Write-Host $m -ForegroundColor Green }
function Warn($m)  { Write-Host $m -ForegroundColor Yellow }
function Die($m)   { Write-Host $m -ForegroundColor Red; throw $m }

function Ensure-Dir($p) { if (-not (Test-Path $p)) { New-Item -ItemType Directory -Path $p -Force | Out-Null } }

# Return $preferred if we can actually bind it, otherwise an OS-assigned free port. Windows reserves
# ranges (WinNAT/Hyper-V) whose ports throw socket error 10013 "access forbidden" on bind — 5xxx ports
# are commonly caught by this — so we fall back rather than crash Kestrel on start.
function Get-BindablePort([int]$preferred) {
    foreach ($candidate in @($preferred, 0)) {
        try {
            $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, $candidate)
            $listener.Start()
            $chosen = ([System.Net.IPEndPoint]$listener.LocalEndpoint).Port
            $listener.Stop()
            return $chosen
        } catch { }
    }
    Die 'Could not find any bindable loopback port.'
}

function Read-State {
    if (Test-Path $StateFile) { try { return Get-Content $StateFile -Raw | ConvertFrom-Json } catch { } }
    return [pscustomobject]@{ ServerPid = 0; Serving = 'none' }
}
function Write-State($state) { Ensure-Dir $HarnessHome; $state | ConvertTo-Json | Set-Content $StateFile }

function Find-SdkTool([string]$name) {
    $roots = @("${env:ProgramFiles(x86)}\Windows Kits\10\bin", "$env:ProgramFiles\Windows Kits\10\bin") |
        Where-Object { $_ -and (Test-Path $_) }
    $hit = foreach ($root in $roots) {
        Get-ChildItem $root -Directory -ErrorAction SilentlyContinue |
            Sort-Object Name -Descending |
            ForEach-Object {
                foreach ($arch in 'x64','x86') {
                    $candidate = Join-Path $_.FullName "$arch\$name"
                    if (Test-Path $candidate) { $candidate }
                }
            }
    }
    $found = $hit | Select-Object -First 1
    if (-not $found) { Die "Could not find $name in the Windows 10 SDK. Install the Windows SDK (includes MakeAppx/SignTool)." }
    return $found
}

# ---------------------------------------------------------------------------- certificate
function Get-TestCert {
    Get-ChildItem Cert:\CurrentUser\My | Where-Object { $_.Subject -eq $CertSubject } | Select-Object -First 1
}

function Test-CertTrusted([string]$thumbprint) {
    $inRoot   = Test-Path "Cert:\LocalMachine\Root\$thumbprint"
    $inPeople = Test-Path "Cert:\LocalMachine\TrustedPeople\$thumbprint"
    return ($inRoot -and $inPeople)
}

function Ensure-Cert {
    $cert = Get-TestCert
    if (-not $cert) {
        Info "Creating self-signed signing cert $CertSubject ..."
        $cert = New-SelfSignedCertificate -Type Custom -Subject $CertSubject `
            -KeyUsage DigitalSignature -CertStoreLocation Cert:\CurrentUser\My `
            -TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.3', '2.5.29.19={text}')
        Ok "Created cert $($cert.Thumbprint)"
    }
    Ensure-Dir $HarnessHome
    Export-Certificate -Cert $cert -FilePath $CerFile -Force | Out-Null

    if (Test-CertTrusted $cert.Thumbprint) {
        Ok "Cert already trusted (machine Root + TrustedPeople)."
        return $cert.Thumbprint
    }

    Warn "Cert not yet trusted. Importing into machine Trusted Root + Trusted People requires admin (one time)."
    $import = @"
Import-Certificate -FilePath '$CerFile' -CertStoreLocation Cert:\LocalMachine\Root | Out-Null
Import-Certificate -FilePath '$CerFile' -CertStoreLocation Cert:\LocalMachine\TrustedPeople | Out-Null
"@
    $enc = [Convert]::ToBase64String([Text.Encoding]::Unicode.GetBytes($import))
    $p = Start-Process pwsh -Verb RunAs -Wait -PassThru -ArgumentList @('-NoProfile','-EncodedCommand',$enc)
    if ($p.ExitCode -ne 0) { Die "Elevated cert import failed (exit $($p.ExitCode))." }
    if (-not (Test-CertTrusted $cert.Thumbprint)) { Die 'Cert import did not take effect.' }
    Ok "Cert trusted."
    return $cert.Thumbprint
}

# ---------------------------------------------------------------------------- build
function Build-Viewer {
    Info 'Building the viewer (populates FwLiteShared/wwwroot/viewer so the MSIX contains the UI)...'
    Push-Location $ViewerDir
    try {
        & pnpm install; if ($LASTEXITCODE) { Die 'pnpm install failed' }
        & pnpm run build; if ($LASTEXITCODE) { Die 'viewer build failed' }
    } finally { Pop-Location }
}

function Build-One([string]$key, [string]$thumbprint) {
    $v = $Versions[$key]
    $makeappx = Find-SdkTool 'makeappx.exe'
    $signtool = Find-SdkTool 'signtool.exe'

    $work      = Join-Path $ArtifactsDir "work\$key"
    $appxDir   = Join-Path $work 'AppPackages'
    $unpacked  = Join-Path $work 'unpacked'
    $bundleIn  = Join-Path $work 'bundle-in'
    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
    Ensure-Dir $appxDir; Ensure-Dir $bundleIn

    Info "[$key] dotnet publish (Release MSIX, $($v.Info)) ..."
    & dotnet publish $MauiCsproj -f $Framework -c Release `
        -p:BuildAndroid=false `
        -p:AppxPackageSigningEnabled=false `
        -p:AppxBundle=Never `
        -p:AppxPackageDir="$appxDir\" `
        -p:ApplicationDisplayVersion=$($v.Semver) `
        -p:InformationalVersion=$($v.Info)
    if ($LASTEXITCODE) { Die "[$key] publish failed" }

    $msix = Get-ChildItem $appxDir -Recurse -Filter *.msix | Sort-Object LastWriteTime -Descending | Select-Object -First 1
    if (-not $msix) { Die "[$key] no .msix produced under $appxDir" }

    Info "[$key] rewriting package identity -> $PackageIdentity $($v.Quad) ..."
    & $makeappx unpack /p $msix.FullName /d $unpacked /o | Out-Null
    if ($LASTEXITCODE) { Die "[$key] makeappx unpack failed" }

    $manifestPath = Join-Path $unpacked 'AppxManifest.xml'
    [xml]$m = Get-Content $manifestPath
    $m.Package.Identity.SetAttribute('Name', $PackageIdentity)
    $m.Package.Identity.SetAttribute('Publisher', $CertSubject)
    $m.Package.Identity.SetAttribute('Version', $v.Quad)
    # Make it visibly the test app in Start/installed-apps (Properties/DisplayName is in the default ns).
    $m.Package.Properties.DisplayName = 'FieldWorks Lite (Update Test)'
    $m.Save($manifestPath)

    $repacked = Join-Path $bundleIn "$PackageIdentity.msix"
    & $makeappx pack /d $unpacked /p $repacked /o /nv | Out-Null
    if ($LASTEXITCODE) { Die "[$key] makeappx pack failed" }

    Ensure-Dir $ArtifactsDir
    $bundleOut = Join-Path $ArtifactsDir $v.Bundle
    Remove-Item $bundleOut -Force -ErrorAction SilentlyContinue
    & $makeappx bundle /d $bundleIn /p $bundleOut /bv $v.Quad /o | Out-Null
    if ($LASTEXITCODE) { Die "[$key] makeappx bundle failed" }

    Info "[$key] signing bundle ..."
    & $signtool sign /fd SHA256 /sha1 $thumbprint $bundleOut | Out-Null
    if ($LASTEXITCODE) { Die "[$key] signtool sign failed" }

    Remove-Item $work -Recurse -Force -ErrorAction SilentlyContinue
    Ok "[$key] built $bundleOut"
}

function Build-Both([switch]$ForceBuild) {
    $existing = $Versions.Values | Where-Object { Test-Path (Join-Path $ArtifactsDir $_.Bundle) }
    if ($existing -and $existing.Count -eq $Versions.Count -and -not $ForceBuild) {
        Warn "Both bundles already exist in $ArtifactsDir :"
        $Versions.Keys | ForEach-Object { Write-Host "   $_ -> $($Versions[$_].Bundle)" }
        if ((Read-Host 'Rebuild anyway? (y/N)') -notmatch '^[Yy]') { return }
    }
    $thumbprint = Ensure-Cert
    Build-Viewer
    foreach ($key in $Versions.Keys) { Build-One $key $thumbprint }

    $manifest = [ordered]@{}
    foreach ($key in $Versions.Keys) {
        $manifest[$key] = @{ version = $Versions[$key].Info; file = $Versions[$key].Bundle }
    }
    $manifest | ConvertTo-Json | Set-Content $BuildsJson
    Ok "Wrote $BuildsJson"
}

# ---------------------------------------------------------------------------- install / launch
function Get-InstalledTestPackage { Get-AppxPackage -Name $PackageIdentity -ErrorAction SilentlyContinue }

function Install-Version([string]$key) {
    $bundle = Join-Path $ArtifactsDir $Versions[$key].Bundle
    if (-not (Test-Path $bundle)) { Die "$key not built yet (missing $bundle). Build first." }
    Info "Installing $key ($($Versions[$key].Info)) ..."
    Add-AppxPackage -Path $bundle
    $pkg = Get-InstalledTestPackage
    Ok "Installed $($pkg.PackageFullName)"
}

function Uninstall-App {
    $pkg = Get-InstalledTestPackage
    if (-not $pkg) { Warn 'Test app is not installed.'; return }
    Info "Uninstalling $($pkg.PackageFullName) ..."
    $pkg | Remove-AppxPackage
    Ok 'Uninstalled. (Your production FwLiteDesktop install is untouched.)'
}

function Launch-App {
    $pkg = Get-InstalledTestPackage
    if (-not $pkg) { Die 'Test app is not installed. Install v1 or v2 first.' }
    Info "Launching $($pkg.PackageFamilyName) ..."
    Start-Process "shell:AppsFolder\$($pkg.PackageFamilyName)!$AppId"
}

# ---------------------------------------------------------------------------- server
function Server-Running([int]$serverPid) { $serverPid -gt 0 -and (Get-Process -Id $serverPid -ErrorAction SilentlyContinue) }

function Start-Server {
    $state = Read-State
    if (Server-Running $state.ServerPid) { Warn "Server already running (pid $($state.ServerPid), serving $($state.Serving)). Stop it first."; return }

    $choice = Read-Host 'Serve which version as the update? (v1 / v2 / none)'
    if ($choice -notin @('v1','v2','none')) { Warn 'Invalid choice.'; return }
    if ($choice -ne 'none' -and -not (Test-Path (Join-Path $ArtifactsDir $Versions[$choice].Bundle))) {
        Die "$choice not built yet. Build first."
    }

    Info 'Building UpdateTestServer (Release) ...'
    & dotnet build $ServerProj -c Release | Out-Null
    if ($LASTEXITCODE) { Die 'server build failed' }

    Ensure-Dir $HarnessHome
    Remove-Item $ServerOutLog, $ServerErrLog -Force -ErrorAction SilentlyContinue
    $p = Start-Process $ServerExe -PassThru -WindowStyle Hidden `
        -RedirectStandardOutput $ServerOutLog -RedirectStandardError $ServerErrLog `
        -ArgumentList @('--port', "$Port", '--serve', $choice, '--bundle-dir', $ArtifactsDir)

    # Give it a moment, then make sure it didn't die on startup (e.g. port bind failure).
    Start-Sleep -Milliseconds 1500
    if ($p.HasExited) {
        Warn "Server exited immediately (code $($p.ExitCode)). Log:"
        Get-Content $ServerOutLog, $ServerErrLog -ErrorAction SilentlyContinue | Select-Object -Last 20 | ForEach-Object { Write-Host "   $_" }
        $state.ServerPid = 0; $state.Serving = 'none'; Write-State $state
        return
    }
    $state.ServerPid = $p.Id; $state.Serving = $choice; Write-State $state
    Ok "Server started (pid $($p.Id)) at http://localhost:$Port serving '$choice'. Log: $ServerOutLog"
}

function Stop-Server {
    $state = Read-State
    if (-not (Server-Running $state.ServerPid)) { Warn 'Server not running.'; $state.ServerPid = 0; Write-State $state; return }
    Info "Stopping server (pid $($state.ServerPid)) ..."
    Stop-Process -Id $state.ServerPid -Force -ErrorAction SilentlyContinue
    $state.ServerPid = 0; $state.Serving = 'none'; Write-State $state
    Ok 'Server stopped.'
}

# ---------------------------------------------------------------------------- update env vars
function Set-UpdateEnv {
    $mode = Read-Host "Update check mode? (Never = drive via in-app dialog [default], Always = auto-check on launch)"
    if ([string]::IsNullOrWhiteSpace($mode)) { $mode = 'Never' }
    if ($mode -notin @('Never','Always')) { Warn 'Invalid mode.'; return }
    $url = Get-UpdateUrl
    [Environment]::SetEnvironmentVariable('FwLite__UpdateUrl', $url, 'User')
    [Environment]::SetEnvironmentVariable('FwLite__UpdateCheckCondition', $mode, 'User')
    Ok "Set (User scope): FwLite__UpdateUrl=$url ; FwLite__UpdateCheckCondition=$mode"
    Warn 'Relaunch the app (option 8) so it picks up these env vars.'
}

function Clear-UpdateEnv {
    [Environment]::SetEnvironmentVariable('FwLite__UpdateUrl', $null, 'User')
    [Environment]::SetEnvironmentVariable('FwLite__UpdateCheckCondition', $null, 'User')
    Ok 'Cleared FwLite__UpdateUrl / FwLite__UpdateCheckCondition (User scope).'
}

# ---------------------------------------------------------------------------- menu
function Show-Status {
    $pkg = Get-InstalledTestPackage
    $state = Read-State
    $envUrl = [Environment]::GetEnvironmentVariable('FwLite__UpdateUrl', 'User')
    $envMode = [Environment]::GetEnvironmentVariable('FwLite__UpdateCheckCondition', 'User')
    $built = ($Versions.Keys | Where-Object { Test-Path (Join-Path $ArtifactsDir $Versions[$_].Bundle) }) -join ', '

    Write-Host ''
    Write-Host '==== FwLite Update Test Harness ====' -ForegroundColor Magenta
    Write-Host ("  Port          : {0}" -f $Port)
    Write-Host ("  Bundles built : {0}" -f ($(if ($built) { $built } else { '(none)' })))
    Write-Host ("  Installed     : {0}" -f ($(if ($pkg) { "$PackageIdentity $($pkg.Version)" } else { '(not installed)' })))
    Write-Host ("  Server        : {0}" -f ($(if (Server-Running $state.ServerPid) { "running pid $($state.ServerPid), serving '$($state.Serving)' on :$Port" } else { 'stopped' })))
    Write-Host ("  Update env    : {0}" -f ($(if ($envUrl) { "URL set, mode=$envMode" } else { 'not set' })))
    Write-Host '------------------------------------'
}

function Show-Menu {
    Write-Host @'
  1) Build both MSIX bundles (v1 low + v2 high)
  2) Create & trust signing cert
  3) Install v1 (lower)      4) Install v2 (higher)
  5) Uninstall test app
  6) Start server            7) Stop server
  8) Launch app
  9) Set update env vars    10) Clear update env vars
  0) Exit
'@
}

Ensure-Dir $ArtifactsDir
# Resolve a usable port up front so the server and the env-var URL always agree, and so we sidestep
# Windows' reserved/excluded port ranges (which crash Kestrel on bind).
$resolved = Get-BindablePort $Port
if ($resolved -ne $Port) { Warn "Port $Port isn't bindable (likely a Windows reserved range); using $resolved instead." }
$Port = $resolved
if ($Force) { Build-Both -ForceBuild }

while ($true) {
    Show-Status
    Show-Menu
    switch (Read-Host 'Choose') {
        '1'  { Build-Both }
        '2'  { Ensure-Cert | Out-Null }
        '3'  { Install-Version 'v1' }
        '4'  { Install-Version 'v2' }
        '5'  { Uninstall-App }
        '6'  { Start-Server }
        '7'  { Stop-Server }
        '8'  { Launch-App }
        '9'  { Set-UpdateEnv }
        '10' { Clear-UpdateEnv }
        '0'  {
            $state = Read-State
            if (Server-Running $state.ServerPid) { if ((Read-Host 'Stop the running server before exit? (Y/n)') -notmatch '^[Nn]') { Stop-Server } }
            return
        }
        default { Warn 'Unknown choice.' }
    }
}

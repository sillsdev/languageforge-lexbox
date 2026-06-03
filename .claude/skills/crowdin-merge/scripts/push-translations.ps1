# push-translations.ps1 — push translations for specified locales to Crowdin, then push branch.
# Runs after user has approved the report and any agent-proposed fixes have been committed.
#
# Usage:
#   .\push-translations.ps1 -Locales ms,sw           # push only these locales, then push branch
#   .\push-translations.ps1 -Locales ms,sw -DryRun   # show what would happen
#
# Locale codes: accept the 2-letter forms that match our .po filenames (es, fr, id, ko, ms, sw, vi).
# The Crowdin project uses `es-ES` for Spanish but bare 2-letter codes for everything else; we
# translate via $CrowdinCodeMap below. If a future locale is added with a different mapping,
# extend this map.
#
# Notes:
#   - No --auto-approve-imported flag. Translations enter Crowdin as unapproved suggestions.
#     The project's Export-Only-Approved is OFF, so suggestions still appear in exports.
#   - Pushes Crowdin first; if that fails the branch hasn't moved and the operation is retryable.

param(
    [Parameter(Mandatory=$true)][string[]]$Locales,
    [switch]$DryRun
)

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

if (-not (Test-Path "crowdin/.crowdin-env.local")) {
    Write-Error "crowdin/.crowdin-env.local not found. Crowdin CLI auth required."
    exit 1
}

# Map .po-filename locale codes to the codes the Crowdin project actually uses.
# Verified via `crowdin status translation`. If push fails with "Language 'X' doesn't
# exist in the project", re-check this map.
$CrowdinCodeMap = @{
    'es' = 'es-ES'
    'fr' = 'fr'
    'id' = 'id'
    'ko' = 'ko'
    'ms' = 'ms'
    'sw' = 'sw'
    'vi' = 'vi'
}

Push-Location crowdin
try {
    foreach ($locale in $Locales) {
        $crowdinLocale = if ($CrowdinCodeMap.ContainsKey($locale)) { $CrowdinCodeMap[$locale] } else { $locale }
        $crowdinArgs = @('--', 'push', 'translations', '-l', $crowdinLocale)
        if ($DryRun) { $crowdinArgs += '--dryrun' }
        if ($crowdinLocale -ne $locale) {
            Write-Host "task crowdin $($crowdinArgs -join ' ')  (mapped from $locale)"
        } else {
            Write-Host "task crowdin $($crowdinArgs -join ' ')"
        }
        task crowdin @crowdinArgs
        if ($LASTEXITCODE -ne 0) {
            Write-Error "Crowdin push failed for $locale ($crowdinLocale). Branch NOT pushed. Fix and retry."
            exit 1
        }
    }
}
finally { Pop-Location }

if ($DryRun) {
    Write-Host "`nDry run complete. Re-run without -DryRun to push for real." -ForegroundColor Yellow
    exit 0
}

Write-Host "`nPushing l10n_develop branch..." -ForegroundColor Cyan
git push origin l10n_develop
if ($LASTEXITCODE -ne 0) {
    Write-Error "Branch push failed. Crowdin already received the translations — just retry git push."
    exit 1
}

Write-Host "`nDone. PR should now be mergeable. Merge via gh pr merge or the GitHub UI." -ForegroundColor Green

# merge-crowdin.ps1 — deterministic git/extract phase of the /crowdin-merge skill.
#
# Pre-flight (silent setup):
#   - Verify clean working tree
#   - Fetch and find the open l10n_develop PR
#
# Numbered steps (shown in script output):
#   [1/6] Pre-flight audit (audit-po.mjs) — abort if any data loss
#   [2/6] Checkout l10n_develop and align with origin (handles Crowdin's force-pushes)
#   [3/6] Merge origin/develop; resolve .po + description-*.md conflicts with --ours
#   [4/6] pnpm i18n:extract in frontend/viewer/
#   [5/6] Stability check (run extract a second time, assert no diff)
#   [6/6] Commit "Merge develop into l10n_develop and reconcile catalogs"
#
# Exits non-zero on any failure with a clear message. Caller (SKILL.md) interprets exit code.

$ErrorActionPreference = 'Stop'
$repoRoot = (git rev-parse --show-toplevel).Trim()
Set-Location $repoRoot

function Fail($msg) { Write-Error $msg; exit 1 }

# Pre-flight: Clean tree
$status = (git status --porcelain)
if ($status) {
    Fail "Working tree is not clean. Commit or stash before running."
}

# Pre-flight: Fetch and find PR
git fetch origin l10n_develop develop 2>&1 | Out-Null
$pr = gh pr list --head l10n_develop --json number,state,headRefName,baseRefName --limit 1 | ConvertFrom-Json
if (-not $pr -or $pr.Count -eq 0) {
    Fail "No open PR found with head l10n_develop. Has Crowdin synced yet?"
}
Write-Host "Found PR #$($pr[0].number): $($pr[0].headRefName) -> $($pr[0].baseRefName)"

# 1. Pre-flight audit
Write-Host "`n[1/6] Auditing for translation deletions..." -ForegroundColor Cyan
node .claude/skills/crowdin-merge/scripts/audit-po.mjs
if ($LASTEXITCODE -ne 0) { Fail "Audit detected data loss. Aborting." }

# 2. Checkout l10n_develop and align with origin (Crowdin force-pushes this branch, so
#    local can be diverged from origin even though we never deliberately commit to it).
Write-Host "`n[2/6] Checking out l10n_develop..." -ForegroundColor Cyan
git checkout l10n_develop 2>&1 | Out-Null

# Check for divergence: are there local-only commits on l10n_develop?
$localOnly = git log l10n_develop "^origin/l10n_develop" --oneline
if ($localOnly) {
    Write-Host "`nLocal l10n_develop has commits not on origin:" -ForegroundColor Yellow
    $localOnly | ForEach-Object { Write-Host "  $_" -ForegroundColor Yellow }

    # Classify: are they all auto-sync commits from Crowdin (safe to drop)?
    $authored = git log l10n_develop "^origin/l10n_develop" --format='%s' |
        Where-Object { $_ -notmatch '^New translations ' -and $_ -notmatch '^\[ci skip\]' }
    if ($authored) {
        Write-Host "`nNON-Crowdin commits found locally — these may be your work:" -ForegroundColor Red
        $authored | ForEach-Object { Write-Host "  $_" -ForegroundColor Red }
        Fail "Refusing to reset. Investigate the commits above; if they should be on a different branch, cherry-pick them first, then re-run."
    }

    Write-Host "`nAll local-only commits are Crowdin auto-syncs (Crowdin force-pushed the branch on a prior cycle)." -ForegroundColor Cyan
    Write-Host "Resetting local l10n_develop to origin/l10n_develop." -ForegroundColor Cyan
    git reset --hard origin/l10n_develop 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "git reset --hard failed. Check working tree state." }
} else {
    git pull --ff-only origin l10n_develop 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { Fail "git pull --ff-only failed. Check network or branch state." }
}

# 3. Merge origin/develop; resolve conflicts with --ours
Write-Host "`n[3/6] Merging origin/develop..." -ForegroundColor Cyan
# Use --no-commit so we can resolve conflicts before committing
git merge --no-commit --no-ff origin/develop 2>&1 | Out-Null
# Merge may exit non-zero on conflicts — that's expected. Continue and resolve.

# Resolve conflicts: prefer Crowdin's side (--ours when merging develop into l10n_develop)
$conflictPatterns = @(
    'frontend/viewer/src/locales/*.po',
    'platform.bible-extension/assets/descriptions/description-*.md'
)
foreach ($p in $conflictPatterns) {
    $files = git diff --name-only --diff-filter=U -- $p
    foreach ($f in $files) {
        if ($f) {
            Write-Host "  resolving (--ours): $f"
            git checkout --ours -- $f
            git add -- $f
        }
    }
}

# Any remaining unresolved conflicts are non-i18n — bail and let the user handle.
$remaining = git diff --name-only --diff-filter=U
if ($remaining) {
    Fail "Unresolved non-i18n conflicts. Resolve manually and re-run:`n$remaining"
}

# 4. Extract — first pass
Write-Host "`n[4/6] Running pnpm i18n:extract..." -ForegroundColor Cyan
Push-Location frontend/viewer
try {
    pnpm i18n:extract 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "i18n:extract failed" }
}
finally { Pop-Location }
git add frontend/viewer/src/locales/

# 5. Stability check — second pass should produce no diff
Write-Host "`n[5/6] Stability check (extract twice)..." -ForegroundColor Cyan
Push-Location frontend/viewer
try {
    pnpm i18n:extract 2>&1 | Out-Null
    if ($LASTEXITCODE -ne 0) { throw "i18n:extract (second pass) failed" }
}
finally { Pop-Location }
$drift = git diff --name-only frontend/viewer/src/locales/
if ($drift) {
    Fail "i18n:extract is not stable — second run produced changes in:`n$drift`nInvestigate before agents run."
}

# 6. Commit
Write-Host "`n[6/6] Committing merge..." -ForegroundColor Cyan
git commit -m "Merge develop into l10n_develop and reconcile catalogs" 2>&1 | Out-Null
if ($LASTEXITCODE -ne 0) { Fail "git commit failed (nothing to commit, or index error)." }

Write-Host "`nMerge phase complete. HEAD: $(git rev-parse --short HEAD)" -ForegroundColor Green
Write-Host "Next: orchestrator runs context-writer + translation-reviewer agents."

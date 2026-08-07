param(
    [string]$Path = 'frontend/viewer/src/lib/dotnet-types/generated-types'
)

$status = git status --porcelain -- $Path
if ($status) {
    Write-Host 'Generated TypeScript types are out of date.'
    Write-Host 'Run: dotnet build backend/FwLite/FwLiteShared/FwLiteShared.csproj'
    Write-Host "Then commit changes under $Path/"
    Write-Host ''
    Write-Host $status
    git diff -- $Path
    exit 1
}

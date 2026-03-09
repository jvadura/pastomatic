# Pastomatic - Publish
# Run from repo root: .\publish.ps1

$ProjectPath = "$PSScriptRoot\Pastomatic\Pastomatic.csproj"
$PublishDir = "$PSScriptRoot\publish"

Write-Host "Publishing Pastomatic..." -ForegroundColor Cyan

dotnet publish $ProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -o $PublishDir

if ($LASTEXITCODE -eq 0) {
    # Copy local settings to publish dir if they exist (won't be overwritten on next publish)
    $localSettings = "$PSScriptRoot\Pastomatic\appsettings.local.json"
    $publishSettings = "$PublishDir\appsettings.local.json"

    if (Test-Path $localSettings) {
        if (-not (Test-Path $publishSettings)) {
            Copy-Item $localSettings $publishSettings
            Write-Host "Copied appsettings.local.json to publish dir" -ForegroundColor Green
        } else {
            Write-Host "appsettings.local.json already exists in publish dir (not overwritten)" -ForegroundColor Yellow
        }
    }

    Write-Host "`nPublished successfully!" -ForegroundColor Green
    Write-Host "  Output: $PublishDir" -ForegroundColor White
} else {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

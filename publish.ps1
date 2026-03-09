# Pastomatic - Publish as single compressed exe
# Run from repo root: .\publish.ps1

$ProjectPath = "$PSScriptRoot\Pastomatic\Pastomatic.csproj"
$PublishDir = "$PSScriptRoot\publish"

Write-Host "Publishing Pastomatic..." -ForegroundColor Cyan

dotnet publish $ProjectPath `
    -c Release `
    -r win-x64 `
    --self-contained false `
    -p:PublishSingleFile=true `
    -p:EnableCompressionInSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
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

    $exe = Get-Item "$PublishDir\Pastomatic.exe"
    Write-Host "`nPublished successfully!" -ForegroundColor Green
    Write-Host "  Output: $PublishDir\Pastomatic.exe" -ForegroundColor White
    Write-Host "  Size:   $([math]::Round($exe.Length / 1MB, 1)) MB" -ForegroundColor White
} else {
    Write-Host "Publish failed!" -ForegroundColor Red
    exit 1
}

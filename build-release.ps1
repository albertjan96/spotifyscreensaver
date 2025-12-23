# Quick Release Script for Spotify Screensaver
# Creates a release-ready package

param(
    [string]$Version = "1.0"
)

Write-Host "?? Building Spotify Now Playing Screensaver Release" -ForegroundColor Cyan
Write-Host ""

# Ensure NuGet.org is available
Write-Host "?? Checking NuGet sources..." -ForegroundColor Yellow
$sources = dotnet nuget list source
if ($sources -notmatch "nuget.org") {
    Write-Host "  ??  Adding NuGet.org source..." -ForegroundColor Yellow
    dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org 2>$null
}
Write-Host "  ? NuGet sources configured" -ForegroundColor Green

# Clean previous builds
Write-Host "?? Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release --verbosity quiet
if (Test-Path "release") { Remove-Item -Recurse -Force "release" }
if (Test-Path "publish") { Remove-Item -Recurse -Force "publish" }

# Build and Publish Self-Contained
Write-Host "?? Building self-contained Release..." -ForegroundColor Yellow
dotnet publish SpotifyNowPlayingScreensaver.csproj `
    --configuration Release `
    --output ./publish `
    --self-contained true `
    --runtime win-x64 `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    /p:PublishReadyToRun=false `
    --verbosity quiet

if ($LASTEXITCODE -ne 0) {
    Write-Host "? Build failed!" -ForegroundColor Red
    exit 1
}

# Get version from file
if (Test-Path "version.txt") {
    $BuildNumber = Get-Content "version.txt"
    $FullVersion = "$Version.$BuildNumber"
} else {
    $FullVersion = "$Version.0"
}

Write-Host "?? Creating release package v$FullVersion..." -ForegroundColor Yellow

# Create release folder
New-Item -ItemType Directory -Force -Path "release" | Out-Null

# Copy and rename published exe to .scr
$exePath = "publish\SpotifyNowPlayingScreensaver.exe"
if (Test-Path $exePath) {
    Copy-Item $exePath "release\SpotifyNowPlayingScreensaver.scr"
    Write-Host "  ? Created SpotifyNowPlayingScreensaver.scr" -ForegroundColor Green
} else {
    Write-Host "  ? Published executable not found!" -ForegroundColor Red
    exit 1
}

# Copy README
Copy-Item "README.md" "release\"
Write-Host "  ? Copied README.md" -ForegroundColor Green

# Copy LICENSE if exists
if (Test-Path "LICENSE") {
    Copy-Item "LICENSE" "release\"
    Write-Host "  ? Copied LICENSE" -ForegroundColor Green
}

# Create ZIP
$zipName = "SpotifyNowPlayingScreensaver-v$FullVersion.zip"
Compress-Archive -Path "release\*" -DestinationPath $zipName -Force
Write-Host "  ? Created $zipName" -ForegroundColor Green

# Get file sizes
$scrSize = (Get-Item "release\SpotifyNowPlayingScreensaver.scr").Length / 1MB
$zipSize = (Get-Item $zipName).Length / 1MB

Write-Host ""
Write-Host "? Release package ready!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Package contents:" -ForegroundColor Cyan
Write-Host "  • SpotifyNowPlayingScreensaver.scr ($([math]::Round($scrSize, 2)) MB - self-contained)"
Write-Host "  • README.md"
if (Test-Path "release\LICENSE") { Write-Host "  • LICENSE" }
Write-Host ""
Write-Host "?? ZIP file: $zipName ($([math]::Round($zipSize, 2)) MB)" -ForegroundColor Cyan
Write-Host "?? Release folder: .\release\" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the .scr file: .\release\SpotifyNowPlayingScreensaver.scr /s"
Write-Host "  2. Test config mode: .\release\SpotifyNowPlayingScreensaver.scr /c"
Write-Host "  3. Create a GitHub release:"
Write-Host "     git tag v$FullVersion"
Write-Host "     git push origin v$FullVersion"
Write-Host "  4. Or upload manually to: https://github.com/albertjan96/spotifyscreensaver/releases/new"
Write-Host ""

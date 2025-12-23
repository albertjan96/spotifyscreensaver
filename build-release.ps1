# Quick Release Script for Spotify Screensaver
# Creates a release-ready package

param(
    [string]$Version = "1.0"
)

Write-Host "?? Building Spotify Now Playing Screensaver Release" -ForegroundColor Cyan
Write-Host ""

# Clean previous builds
Write-Host "?? Cleaning previous builds..." -ForegroundColor Yellow
dotnet clean --configuration Release --verbosity quiet
if (Test-Path "release") { Remove-Item -Recurse -Force "release" }

# Build Release
Write-Host "?? Building Release configuration..." -ForegroundColor Yellow
dotnet build --configuration Release --verbosity quiet

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

# Copy screensaver file
$scrPath = "bin\Release\net8.0-windows\SpotifyNowPlayingScreensaver.scr"
if (Test-Path $scrPath) {
    Copy-Item $scrPath "release\"
    Write-Host "  ? Copied SpotifyNowPlayingScreensaver.scr" -ForegroundColor Green
} else {
    Write-Host "  ? Screensaver file not found!" -ForegroundColor Red
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
$scrSize = (Get-Item $scrPath).Length / 1KB
$zipSize = (Get-Item $zipName).Length / 1KB

Write-Host ""
Write-Host "? Release package ready!" -ForegroundColor Green
Write-Host ""
Write-Host "?? Package contents:" -ForegroundColor Cyan
Write-Host "  • SpotifyNowPlayingScreensaver.scr ($([math]::Round($scrSize, 1)) KB)"
Write-Host "  • README.md"
if (Test-Path "release\LICENSE") { Write-Host "  • LICENSE" }
Write-Host ""
Write-Host "?? ZIP file: $zipName ($([math]::Round($zipSize, 1)) KB)" -ForegroundColor Cyan
Write-Host "?? Release folder: .\release\" -ForegroundColor Cyan
Write-Host ""
Write-Host "?? Next steps:" -ForegroundColor Yellow
Write-Host "  1. Test the .scr file from the release folder"
Write-Host "  2. Create a GitHub release: gh release create v$FullVersion $zipName --title 'v$FullVersion'"
Write-Host "  3. Or upload manually to: https://github.com/albertjan96/spotifyscreensaver/releases/new"
Write-Host ""

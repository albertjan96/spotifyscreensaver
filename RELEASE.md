# How to Create a Release

## Method 1: Automated via GitHub Actions ⭐ (Recommended)

### Option A: Create Tag Locally and Push
```powershell
# Ensure all changes are committed
git add .
git commit -m "Ready for release"
git push

# Create and push version tag (triggers automatic build)
git tag v1.0.X
git push origin v1.0.X
```

### Option B: Manual Workflow Dispatch
1. Go to: https://github.com/albertjan96/spotifyscreensaver/actions
2. Click "Build Release" workflow
3. Click "Run workflow"
4. Select branch and click "Run workflow"

GitHub Actions will automatically:
- ✅ Build self-contained Release
- ✅ Create `.scr` file (~60 MB)
- ✅ Package into ZIP
- ✅ Create GitHub Release
- ✅ Upload artifacts

## Method 2: Manual Local Build

### Quick Build
```powershell
# Run the automated build script
.\build-release.ps1

# This creates:
# - release\SpotifyNowPlayingScreensaver.scr (~60 MB)
# - SpotifyNowPlayingScreensaver-v1.0.X.zip
```

### Build Details
The script performs:
1. Checks NuGet sources
2. Cleans previous builds
3. Publishes self-contained single-file executable
4. Copies and renames to `.scr`
5. Creates release package and ZIP

### Test the Build
```powershell
# Test screensaver mode
.\release\SpotifyNowPlayingScreensaver.scr /s

# Test configuration mode
.\release\SpotifyNowPlayingScreensaver.scr /c
```

### Manual GitHub Release Upload
1. Go to: https://github.com/albertjan96/spotifyscreensaver/releases/new
2. Create tag: `v1.0.X`
3. Title: `Spotify Now Playing Screensaver v1.0.X`
4. Upload the ZIP file
5. Add release notes (see template below)
6. Publish

## Release Notes Template

```markdown
## 🎵 Spotify Now Playing Screensaver v1.0.X

**AI-Generated Project** - Developed entirely with GitHub Copilot assistance.

### 📥 Installation
1. Download `SpotifyNowPlayingScreensaver.scr` (~60 MB)
2. Right-click → "Install" or copy to `C:\Windows\System32\`
3. Configure via Windows Screen Saver settings

### ✨ Features
- Real-time Spotify track display
- Album artwork & playback time
- Next track preview
- Multi-monitor support
- Self-contained (no .NET installation required!)

### 📋 Requirements
- Windows 10/11 (64-bit)
- Spotify Developer App (see README for setup)

### 🆕 What's New
- Self-contained build - no .NET runtime needed
- Single file distribution
- [Add specific changes here]
```

## Build Configuration

Both methods use the same build settings:
```
--configuration Release
--self-contained true
--runtime win-x64
-p:PublishSingleFile=true
-p:IncludeNativeLibrariesForSelfExtract=true
/p:PublishReadyToRun=false
```

## What Gets Released?

**Package Contents:**
- ✅ `SpotifyNowPlayingScreensaver.scr` (~60 MB self-contained)
- ✅ `README.md`
- ✅ `LICENSE` (if exists)

**Why ~60 MB?**
- Includes complete .NET 8 runtime
- No external dependencies required
- Single-file deployment

## Troubleshooting

### Build Fails Locally
```powershell
# Check NuGet sources
dotnet nuget list source

# Add NuGet.org if missing
dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org

# Try restore explicitly
dotnet restore --source https://api.nuget.org/v3/index.json
```

### GitHub Actions Fails
1. Check workflow permissions (Settings → Actions → General)
2. Ensure "Read and write permissions" is enabled
3. Check NuGet source configuration in workflow

## Release Checklist

- [ ] All code committed and pushed
- [ ] README.md is up to date
- [ ] Version number correct in `version.txt`
- [ ] Test screensaver locally (config + fullscreen)
- [ ] Build succeeds without errors
- [ ] `.scr` file works on clean Windows install
- [ ] Create Git tag
- [ ] Push tag to trigger workflow
- [ ] Verify GitHub release created
- [ ] Test download from GitHub

## Version Numbering

Format: `v1.0.{build}`
- Major: 1
- Minor: 0
- Build: Auto-incremented in `version.txt`

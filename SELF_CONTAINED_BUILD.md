# Self-Contained Release Build

## What Changed

### Problem
The screensaver `.scr` file needed additional .NET runtime files to work, making distribution complicated.

### Solution
Changed to **self-contained, single-file** build:
- ? Includes .NET 8 runtime
- ? Single `.scr` file (~60 MB)
- ? No .NET installation required on target machine
- ? Works standalone

## Updated Files

### 1. `.github/workflows/release.yml`
- Added `dotnet publish` with self-contained flags
- Changed to copy published `.exe` and rename to `.scr`

### 2. `build-release.ps1`
- Updated to create self-contained build
- Copies from `publish/` folder instead of `bin/Release/`

### 3. `README.md`
- Updated installation instructions
- Mentions self-contained build (~60 MB)
- Notes that .NET is included

## Build Configuration

```powershell
dotnet publish SpotifyNowPlayingScreensaver.csproj `
  --configuration Release `
  --output ./publish `
  --self-contained true `
  --runtime win-x64 `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

## File Size Comparison

| Build Type | Size | .NET Required | Files |
|------------|------|---------------|-------|
| **Old (Framework-dependent)** | ~150 KB | Yes | Multiple DLLs |
| **New (Self-contained)** | ~60 MB | No | Single .scr |

## Benefits

? **Easy Distribution** - Single file  
? **No Dependencies** - Works on any Windows 10/11  
? **User Friendly** - Just download and install  
? **Professional** - Like commercial screensavers  

## Testing

```powershell
# Build locally
.\build-release.ps1

# Test the screensaver
.\release\SpotifyNowPlayingScreensaver.scr /s

# Check file size
Get-Item .\release\SpotifyNowPlayingScreensaver.scr | Select Length
```

## GitHub Actions

The workflow now:
1. Builds solution
2. **Publishes self-contained single-file**
3. Copies published `.exe` ? `.scr`
4. Creates ZIP package
5. Uploads as artifact
6. Creates GitHub release

Ready to push and test!

# How to Create a Release

## Method 1: GitHub Actions (Automated)

1. **Commit all your changes**:
   ```powershell
   git add .
   git commit -m "Prepare for release"
   git push
   ```

2. **Create and push a version tag**:
   ```powershell
   git tag v1.0.0
   git push origin v1.0.0
   ```

3. GitHub Actions will automatically:
   - Build the Release version
   - Create a `.scr` file
   - Package it into a zip
   - Create a GitHub Release
   - Attach the files

## Method 2: Manual Local Build

### Step 1: Build Release Version

```powershell
# Clean previous builds
dotnet clean --configuration Release

# Build Release
dotnet build --configuration Release

# The .scr file is in: bin\Release\net8.0-windows\
```

### Step 2: Create Release Package

```powershell
# Create release folder
New-Item -ItemType Directory -Force -Path release

# Copy only necessary files
Copy-Item "bin\Release\net8.0-windows\SpotifyNowPlayingScreensaver.scr" "release\"
Copy-Item "README.md" "release\"
Copy-Item "LICENSE" "release\" -ErrorAction SilentlyContinue

# Create ZIP
Compress-Archive -Path release\* -DestinationPath "SpotifyNowPlayingScreensaver-v1.0.zip" -Force
```

### Step 3: Upload to GitHub

1. Go to: https://github.com/albertjan96/spotifyscreensaver/releases
2. Click "Create a new release"
3. Click "Choose a tag" ? Type `v1.0.0` ? Create new tag
4. Title: `Spotify Now Playing Screensaver v1.0.0`
5. Description:
   ```markdown
   ## ?? Spotify Now Playing Screensaver v1.0.0
   
   **AI-Generated Project** - Developed entirely with GitHub Copilot assistance.
   
   ### Features
   - Real-time Spotify track display
   - Album artwork
   - Next track preview
   - Multi-monitor support
   - OAuth 2.0 + PKCE authentication
   
   ### Installation
   1. Download `SpotifyNowPlayingScreensaver.scr`
   2. Right-click ? "Install"
   3. Or copy to `C:\Windows\System32\`
   4. Configure via Windows Screen Saver settings
   
   ### Requirements
   - Windows 10/11
   - .NET 8.0 Runtime
   - Spotify Developer App (see README)
   
   ### Files
   - `SpotifyNowPlayingScreensaver.scr` - Screensaver file (64 KB)
   - `README.md` - Full documentation
   ```
6. Drag and drop your ZIP file
7. Click "Publish release"

## What Gets Released?

**Single file release** (recommended):
- ? `SpotifyNowPlayingScreensaver.scr` (~64 KB)
- ? `README.md`

**Why so small?**
- .NET 8 is self-contained in the .scr file
- No additional DLLs needed
- User config stored in AppData

## Release Checklist

- [ ] All code committed and pushed
- [ ] README.md is up to date
- [ ] Version number incremented in `version.txt`
- [ ] Test the screensaver works
- [ ] Build Release configuration
- [ ] Test the .scr file on clean Windows
- [ ] Create GitHub release
- [ ] Verify download works

## Version Numbering

Current version is stored in `version.txt`:
- Major: 1
- Minor: 0  
- Build: Auto-incremented

Format: `v1.0.{build}`

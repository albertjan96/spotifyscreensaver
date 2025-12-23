# GitHub Actions Permission Fix

## Problem
GitHub Actions workflow fails with `403` error when trying to create a release.

## Solution

### Option 1: Enable Workflow Permissions (Recommended)

1. Go to your repository on GitHub
2. Click **Settings** ? **Actions** ? **General**
3. Scroll to **Workflow permissions**
4. Select **"Read and write permissions"**
5. Check **"Allow GitHub Actions to create and approve pull requests"**
6. Click **Save**

### Option 2: Use Artifacts Instead

If you don't want to give write permissions, the workflow now also uploads artifacts:

1. Go to the **Actions** tab after a workflow run
2. Download the artifact ZIP
3. Manually create a release:
   - Go to **Releases** ? **Create a new release**
   - Upload the downloaded files

## Testing the Fix

After enabling permissions:

```powershell
# Delete the failed tag locally
git tag -d v1.0.31
git push origin :refs/tags/v1.0.31

# Create a new tag
git tag v1.0.32
git push origin v1.0.32
```

The workflow should now succeed and create the release automatically!

## Alternative: Manual Release

If you prefer full control:

1. Run the local build script:
   ```powershell
   .\build-release.ps1
   ```

2. Go to GitHub ? Releases ? New Release
3. Create tag: `v1.0.X`
4. Upload the ZIP file from the build script
5. Publish

## Current Workflow Features

? Automatic versioning from `version.txt`  
? Builds Release configuration  
? Creates `.scr` file  
? Packages into ZIP  
? Uploads as artifact (backup)  
? Creates GitHub release (if permissions allow)

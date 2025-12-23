# NuGet Package Resolution Fix

## Problem
```
error NU1101: Unable to find package Microsoft.WindowsDesktop.App.Runtime.win-x64
error NU1101: Unable to find package Microsoft.AspNetCore.App.Runtime.win-x64
```

GitHub Actions couldn't find the runtime packages needed for self-contained builds.

## Root Cause
The build was trying to use offline Visual Studio packages instead of NuGet.org.

## Solution Applied

### 1. Configure NuGet Source
Added explicit NuGet.org source configuration:
```yaml
- name: Configure NuGet
  run: |
    dotnet nuget list source
    dotnet nuget add source https://api.nuget.org/v3/index.json --name nuget.org || echo "Source already exists"
```

### 2. Specify Source in Restore
```yaml
- name: Restore dependencies
  run: dotnet restore Scr.sln --source https://api.nuget.org/v3/index.json
```

### 3. Let Publish Handle Dependencies
Removed `--source` from publish command - it will use the restored packages.

### 4. Disable ReadyToRun
Added `/p:PublishReadyToRun=false` for better compatibility.

## Final Workflow Steps

1. ? Checkout code
2. ? Setup .NET 8
3. ? Configure NuGet source
4. ? Restore dependencies from NuGet.org
5. ? Build Release
6. ? Publish self-contained
7. ? Create release package
8. ? Upload artifacts
9. ? Create GitHub release

## Testing

After committing and pushing:

```powershell
# Commit changes
git add .github/workflows/release.yml
git commit -m "Fix NuGet package resolution in GitHub Actions"
git push

# Create and push tag to trigger workflow
git tag v1.0.33
git push origin v1.0.33
```

## Expected Result

? Build succeeds  
? Self-contained .exe created  
? Renamed to .scr (~60 MB)  
? Packaged in ZIP  
? GitHub release created automatically  

## Troubleshooting

If still failing, check:
1. GitHub Actions logs for specific error
2. NuGet sources list: `dotnet nuget list source`
3. Try restoring locally first: `dotnet restore --source https://api.nuget.org/v3/index.json`

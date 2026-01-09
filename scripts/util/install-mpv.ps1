# Download and install mpv with libmpv-2.dll
$ErrorActionPreference = 'Stop'

$mpvUrl = 'https://github.com/shinchiro/mpv-winbuild-cmake/releases/download/20241215/mpv-x86_64-20241215-git-fa927c3.7z'
$downloadPath = "$env:TEMP\mpv.7z"
$extractPath = 'C:\Users\jeffr\Documents\Visual Studio Code Projects\VapourSynth GUI\dist\mpv'

Write-Host 'Downloading mpv from GitHub...'
Invoke-WebRequest -Uri $mpvUrl -OutFile $downloadPath
Write-Host "Downloaded to: $downloadPath"
Write-Host "File size: $((Get-Item $downloadPath).Length) bytes"

# Check if 7-Zip is available
$7zipPath = 'C:\Program Files\7-Zip\7z.exe'
if (-not (Test-Path $7zipPath)) {
    Write-Host '7-Zip not found. Installing via winget...'
    winget install 7zip.7zip --accept-package-agreements --accept-source-agreements
    $7zipPath = 'C:\Program Files\7-Zip\7z.exe'
}

# Create extract directory
if (-not (Test-Path $extractPath)) {
    New-Item -ItemType Directory -Path $extractPath -Force | Out-Null
}

Write-Host 'Extracting mpv...'
& $7zipPath x $downloadPath -o"$extractPath" -y

# Verify libmpv-2.dll exists
$libmpvPath = Join-Path $extractPath 'libmpv-2.dll'
if (Test-Path $libmpvPath) {
    Write-Host "Success! libmpv-2.dll found at: $libmpvPath"
} else {
    # Check subdirectories
    $found = Get-ChildItem -Path $extractPath -Filter 'libmpv-2.dll' -Recurse | Select-Object -First 1
    if ($found) {
        Write-Host "Success! libmpv-2.dll found at: $($found.FullName)"
    } else {
        Write-Host "Warning: libmpv-2.dll not found in extracted files"
        Get-ChildItem -Path $extractPath -Recurse | Select-Object FullName
    }
}

# Clean up
Remove-Item $downloadPath -Force -ErrorAction SilentlyContinue

Write-Host 'Done!'

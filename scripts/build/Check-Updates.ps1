<#
.SYNOPSIS
    Checks for updates to VapourSynth plugins and components.

.DESCRIPTION
    Queries GitHub API to find newer versions of plugins defined in plugins.json.
    Can optionally update plugins.json with new URLs and versions.

.PARAMETER Plugins
    Check for plugin updates only.

.PARAMETER Core
    Check for VapourSynth and Python updates.

.PARAMETER All
    Check all components (default).

.PARAMETER Apply
    Update plugins.json with new versions (requires confirmation).

.PARAMETER Force
    Skip cache and re-fetch all version information.

.EXAMPLE
    .\Check-Updates.ps1
    Check all components for updates.

.EXAMPLE
    .\Check-Updates.ps1 -Plugins -Apply
    Check for plugin updates and apply changes to plugins.json.
#>

[CmdletBinding()]
param(
    [switch]$Plugins,
    [switch]$Core,
    [switch]$All,
    [switch]$Apply,
    [switch]$Force
)

$ErrorActionPreference = "Stop"

# If no specific flag, check all
if (-not $Plugins -and -not $Core) {
    $All = $true
}

# Find project root (where plugins.json is located)
$projectRoot = $PSScriptRoot
for ($i = 0; $i -lt 5; $i++) {
    if (Test-Path (Join-Path $projectRoot "plugins.json")) { break }
    $projectRoot = Split-Path -Parent $projectRoot
}

# Load versions from shared config
$versionsFile = Join-Path $projectRoot "config\versions.json"
if (Test-Path $versionsFile) {
    $versions = Get-Content $versionsFile | ConvertFrom-Json
    $currentPythonVersion = $versions.python.version
    $currentVSVersion = $versions.vapoursynth.version
} else {
    # Fallback to hardcoded versions if config not found
    Write-Warning "versions.json not found at $versionsFile, using defaults"
    $currentPythonVersion = "3.12.4"
    $currentVSVersion = "R68"
}

# Configuration
$script:Config = @{
    ConfigFile = Join-Path $projectRoot "plugins.json"
    CacheFile = Join-Path $projectRoot "build\update-cache.json"
    CacheTTLMinutes = 60
    GitHubApiBase = "https://api.github.com"
    CurrentPythonVersion = $currentPythonVersion
    CurrentVSVersion = $currentVSVersion
}

# Cache management
$script:Cache = @{}

function Get-UpdateCache {
    if (-not $Force -and (Test-Path $script:Config.CacheFile)) {
        $cacheInfo = Get-Item $script:Config.CacheFile
        $age = (Get-Date) - $cacheInfo.LastWriteTime
        if ($age.TotalMinutes -lt $script:Config.CacheTTLMinutes) {
            try {
                $script:Cache = Get-Content $script:Config.CacheFile | ConvertFrom-Json -AsHashtable
                Write-Host "Using cached version data (age: $([int]$age.TotalMinutes) minutes)" -ForegroundColor Gray
                return $true
            }
            catch {
                # Cache corrupted, will refresh
            }
        }
    }
    return $false
}

function Save-UpdateCache {
    $cacheDir = Split-Path $script:Config.CacheFile -Parent
    if (-not (Test-Path $cacheDir)) {
        New-Item -ItemType Directory -Path $cacheDir -Force | Out-Null
    }
    $script:Cache | ConvertTo-Json -Depth 5 | Set-Content $script:Config.CacheFile
}

function Get-GitHubRepoFromUrl {
    param([string]$Url)

    # Extract owner/repo from GitHub URLs
    # Patterns: github.com/owner/repo/releases/...
    if ($Url -match "github\.com/([^/]+)/([^/]+)") {
        return @{
            Owner = $Matches[1]
            Repo = $Matches[2]
        }
    }
    return $null
}

function Get-LatestGitHubRelease {
    param(
        [string]$Owner,
        [string]$Repo
    )

    $cacheKey = "$Owner/$Repo"

    # Check cache first
    if ($script:Cache.ContainsKey($cacheKey)) {
        return $script:Cache[$cacheKey]
    }

    try {
        $apiUrl = "$($script:Config.GitHubApiBase)/repos/$Owner/$Repo/releases/latest"
        $response = Invoke-RestMethod -Uri $apiUrl -Headers @{
            "Accept" = "application/vnd.github.v3+json"
            "User-Agent" = "VapourSynth-Portable-UpdateChecker"
        } -ErrorAction Stop

        $result = @{
            TagName = $response.tag_name
            Name = $response.name
            PublishedAt = $response.published_at
            Assets = @($response.assets | ForEach-Object {
                @{
                    Name = $_.name
                    Url = $_.browser_download_url
                    Size = $_.size
                }
            })
        }

        $script:Cache[$cacheKey] = $result
        return $result
    }
    catch {
        Write-Host "  Could not fetch release info for $Owner/$Repo" -ForegroundColor Yellow
        return $null
    }
}

function Compare-Versions {
    param(
        [string]$Current,
        [string]$Latest
    )

    # Normalize versions (remove common prefixes like 'v', 'r', 'R')
    $currentNorm = $Current -replace '^[vVrR]', ''
    $latestNorm = $Latest -replace '^[vVrR]', ''

    # If they match after normalization, no update needed
    if ($currentNorm -eq $latestNorm) {
        return "current"
    }

    # Try numeric comparison
    try {
        $currentNum = [double]($currentNorm -replace '[^0-9.]', '')
        $latestNum = [double]($latestNorm -replace '[^0-9.]', '')

        if ($latestNum -gt $currentNum) {
            return "outdated"
        }
        elseif ($latestNum -lt $currentNum) {
            return "newer"  # Local is newer than remote (unlikely but possible)
        }
        else {
            return "current"
        }
    }
    catch {
        # Fall back to string comparison
        if ($currentNorm -ne $latestNorm) {
            return "different"
        }
        return "current"
    }
}

function Find-CompatibleAsset {
    param(
        [array]$Assets,
        [string]$PluginName
    )

    # Look for x64/win64 assets
    $compatible = $Assets | Where-Object {
        ($_.Name -match "x64|win64|64bit|amd64") -and
        ($_.Name -match "\.zip$|\.7z$") -and
        -not ($_.Name -match "x86|win32|32bit")
    }

    if ($compatible) {
        return $compatible | Select-Object -First 1
    }

    # Fallback: any zip/7z that's not clearly 32-bit
    $fallback = $Assets | Where-Object {
        ($_.Name -match "\.zip$|\.7z$") -and
        -not ($_.Name -match "x86|win32|32bit")
    }

    return $fallback | Select-Object -First 1
}

function Check-PluginUpdates {
    Write-Host "`n=== Plugin Updates ===" -ForegroundColor Cyan

    if (-not (Test-Path $script:Config.ConfigFile)) {
        Write-Host "plugins.json not found" -ForegroundColor Red
        return @()
    }

    $config = Get-Content $script:Config.ConfigFile | ConvertFrom-Json
    $updates = @()

    foreach ($plugin in $config.plugins) {
        Write-Host "  Checking: $($plugin.name)..." -NoNewline

        $repoInfo = Get-GitHubRepoFromUrl $plugin.url
        if (-not $repoInfo) {
            Write-Host " (not a GitHub URL)" -ForegroundColor Gray
            continue
        }

        $latestRelease = Get-LatestGitHubRelease -Owner $repoInfo.Owner -Repo $repoInfo.Repo
        if (-not $latestRelease) {
            Write-Host " (could not fetch)" -ForegroundColor Yellow
            continue
        }

        $status = Compare-Versions -Current $plugin.version -Latest $latestRelease.TagName

        switch ($status) {
            "outdated" {
                Write-Host " UPDATE AVAILABLE" -ForegroundColor Green
                Write-Host "    Current: $($plugin.version) -> Latest: $($latestRelease.TagName)" -ForegroundColor Yellow

                $asset = Find-CompatibleAsset -Assets $latestRelease.Assets -PluginName $plugin.name
                if ($asset) {
                    $updates += @{
                        Name = $plugin.name
                        CurrentVersion = $plugin.version
                        LatestVersion = $latestRelease.TagName
                        CurrentUrl = $plugin.url
                        NewUrl = $asset.Url
                        Size = $asset.Size
                    }
                }
            }
            "current" {
                Write-Host " up to date ($($plugin.version))" -ForegroundColor Gray
            }
            "different" {
                Write-Host " version differs: $($plugin.version) vs $($latestRelease.TagName)" -ForegroundColor Yellow
            }
            default {
                Write-Host " $($plugin.version)" -ForegroundColor Gray
            }
        }

        # Rate limiting - be nice to GitHub API
        Start-Sleep -Milliseconds 100
    }

    return $updates
}

function Check-CoreUpdates {
    Write-Host "`n=== Core Component Updates ===" -ForegroundColor Cyan

    $updates = @()

    # Check VapourSynth
    Write-Host "  Checking: VapourSynth..." -NoNewline
    $vsLatest = Get-LatestGitHubRelease -Owner "vapoursynth" -Repo "vapoursynth"
    if ($vsLatest) {
        $status = Compare-Versions -Current $script:Config.CurrentVSVersion -Latest $vsLatest.TagName
        if ($status -eq "outdated") {
            Write-Host " UPDATE AVAILABLE" -ForegroundColor Green
            Write-Host "    Current: $($script:Config.CurrentVSVersion) -> Latest: $($vsLatest.TagName)" -ForegroundColor Yellow
            $updates += @{
                Name = "VapourSynth"
                CurrentVersion = $script:Config.CurrentVSVersion
                LatestVersion = $vsLatest.TagName
            }
        }
        else {
            Write-Host " up to date ($($script:Config.CurrentVSVersion))" -ForegroundColor Gray
        }
    }

    # Check Python (via python.org API or hardcoded)
    Write-Host "  Checking: Python..." -NoNewline
    # Python doesn't have a simple GitHub release API, so we'd need to scrape python.org
    # For now, just report current version
    Write-Host " $($script:Config.CurrentPythonVersion) (manual check required)" -ForegroundColor Gray

    return $updates
}

function Apply-Updates {
    param([array]$Updates)

    if ($Updates.Count -eq 0) {
        Write-Host "`nNo updates to apply." -ForegroundColor Gray
        return
    }

    Write-Host "`n=== Applying Updates ===" -ForegroundColor Cyan
    Write-Host "The following plugins will be updated in plugins.json:" -ForegroundColor Yellow

    foreach ($update in $Updates) {
        Write-Host "  - $($update.Name): $($update.CurrentVersion) -> $($update.LatestVersion)"
    }

    $confirm = Read-Host "`nProceed? (y/N)"
    if ($confirm -ne 'y' -and $confirm -ne 'Y') {
        Write-Host "Cancelled." -ForegroundColor Gray
        return
    }

    # Load and update plugins.json
    $config = Get-Content $script:Config.ConfigFile -Raw | ConvertFrom-Json

    foreach ($update in $Updates) {
        $plugin = $config.plugins | Where-Object { $_.name -eq $update.Name }
        if ($plugin -and $update.NewUrl) {
            $plugin.version = $update.LatestVersion
            $plugin.url = $update.NewUrl
            Write-Host "  Updated: $($update.Name)" -ForegroundColor Green
        }
    }

    # Create backup
    $backupPath = "$($script:Config.ConfigFile).bak"
    Copy-Item $script:Config.ConfigFile $backupPath
    Write-Host "  Backup created: $backupPath" -ForegroundColor Gray

    # Save updated config
    $config | ConvertTo-Json -Depth 10 | Set-Content $script:Config.ConfigFile
    Write-Host "`nUpdates applied to plugins.json" -ForegroundColor Green
}

# Main execution
Write-Host "`n========================================" -ForegroundColor Magenta
Write-Host "  VapourSynth Portable Update Checker" -ForegroundColor Magenta
Write-Host "========================================" -ForegroundColor Magenta

# Load cache
Get-UpdateCache | Out-Null

$allUpdates = @()

if ($All -or $Plugins) {
    $pluginUpdates = Check-PluginUpdates
    $allUpdates += $pluginUpdates
}

if ($All -or $Core) {
    $coreUpdates = Check-CoreUpdates
    $allUpdates += $coreUpdates
}

# Save cache
Save-UpdateCache

# Summary
Write-Host "`n=== Summary ===" -ForegroundColor Cyan
if ($allUpdates.Count -gt 0) {
    Write-Host "$($allUpdates.Count) update(s) available" -ForegroundColor Yellow

    if ($Apply) {
        Apply-Updates -Updates ($allUpdates | Where-Object { $_.NewUrl })
    }
    else {
        Write-Host "`nRun with -Apply to update plugins.json" -ForegroundColor Gray
    }
}
else {
    Write-Host "All components are up to date!" -ForegroundColor Green
}

Write-Host ""

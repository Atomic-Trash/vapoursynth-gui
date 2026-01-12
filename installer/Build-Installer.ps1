<#
.SYNOPSIS
    Builds the VapourSynth Studio installer.

.DESCRIPTION
    This script builds the application in Release mode, publishes it as self-contained,
    and then creates an installer using Inno Setup.

.PARAMETER Version
    The version number for the installer (e.g., "1.0.0")

.PARAMETER SkipBuild
    Skip the dotnet build/publish step (use existing build)

.PARAMETER InnoSetupPath
    Path to ISCC.exe (Inno Setup compiler). If not specified, searches common locations.

.EXAMPLE
    .\Build-Installer.ps1 -Version "1.0.0"

.EXAMPLE
    .\Build-Installer.ps1 -Version "1.0.0" -SkipBuild
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$Version,

    [switch]$SkipBuild,

    [string]$InnoSetupPath
)

$ErrorActionPreference = "Stop"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$ProjectRoot = Split-Path -Parent $ScriptDir
$ProjectFile = Join-Path $ProjectRoot "src\gui\VapourSynthPortable\VapourSynthPortable.csproj"
$PublishDir = Join-Path $ProjectRoot "src\gui\VapourSynthPortable\bin\publish\win-x64-self-contained"
$InstallerScript = Join-Path $ScriptDir "VapourSynthStudio.iss"
$OutputDir = Join-Path $ProjectRoot "dist\installer"

Write-Host "VapourSynth Studio Installer Builder" -ForegroundColor Cyan
Write-Host "=====================================" -ForegroundColor Cyan
Write-Host "Version: $Version"
Write-Host ""

# Find Inno Setup compiler
if (-not $InnoSetupPath) {
    $searchPaths = @(
        "${env:ProgramFiles(x86)}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 6\ISCC.exe",
        "${env:ProgramFiles(x86)}\Inno Setup 5\ISCC.exe",
        "${env:ProgramFiles}\Inno Setup 5\ISCC.exe"
    )

    foreach ($path in $searchPaths) {
        if (Test-Path $path) {
            $InnoSetupPath = $path
            break
        }
    }
}

if (-not $InnoSetupPath -or -not (Test-Path $InnoSetupPath)) {
    Write-Error "Inno Setup compiler (ISCC.exe) not found. Please install Inno Setup from https://jrsoftware.org/isinfo.php or specify path with -InnoSetupPath"
    exit 1
}

Write-Host "Using Inno Setup: $InnoSetupPath" -ForegroundColor Gray

# Build and publish
if (-not $SkipBuild) {
    Write-Host ""
    Write-Host "Building and publishing application..." -ForegroundColor Yellow

    # Clean previous publish
    if (Test-Path $PublishDir) {
        Remove-Item $PublishDir -Recurse -Force
    }

    # Publish self-contained
    dotnet publish $ProjectFile `
        -c Release `
        -r win-x64 `
        --self-contained true `
        -p:PublishSingleFile=true `
        -p:PublishReadyToRun=true `
        -p:EnableCompressionInSingleFile=true `
        -p:Version=$Version `
        -o $PublishDir

    if ($LASTEXITCODE -ne 0) {
        Write-Error "Build failed with exit code $LASTEXITCODE"
        exit 1
    }

    Write-Host "Build complete!" -ForegroundColor Green
}

# Create output directory
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

# Build installer
Write-Host ""
Write-Host "Building installer..." -ForegroundColor Yellow

& $InnoSetupPath /DMyAppVersion="$Version" $InstallerScript

if ($LASTEXITCODE -ne 0) {
    Write-Error "Installer build failed with exit code $LASTEXITCODE"
    exit 1
}

Write-Host ""
Write-Host "Installer created successfully!" -ForegroundColor Green
Write-Host "Output: $OutputDir\VapourSynthStudio-$Version-Setup.exe"

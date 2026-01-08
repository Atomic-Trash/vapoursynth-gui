<#
.SYNOPSIS
    Builds a portable VapourSynth distribution for Windows 11.

.DESCRIPTION
    Downloads and packages VapourSynth, Python, and plugins into a
    self-contained portable distribution.

.PARAMETER Clean
    Remove existing build artifacts before building.

.PARAMETER Components
    Comma-separated list of components to build.
    Options: python, vapoursynth, plugins, all (default)

.PARAMETER PluginSet
    Plugin set to install: minimal, standard (default), full

.EXAMPLE
    .\Build-Portable.ps1

.EXAMPLE
    .\Build-Portable.ps1 -Clean -PluginSet full
#>

[CmdletBinding()]
param(
    [switch]$Clean,
    [string]$Components = "all",
    [ValidateSet("minimal", "standard", "full")]
    [string]$PluginSet = "standard"
)

# Configuration
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$script:Config = @{
    PythonVersion = "3.12.4"
    VapourSynthVersion = "R68"
    RootDir = $PSScriptRoot
    BuildDir = Join-Path $PSScriptRoot "build"
    DistDir = Join-Path $PSScriptRoot "dist"
    PythonUrl = "https://www.python.org/ftp/python/{0}/python-{0}-embed-amd64.zip"
    VapourSynthUrl = "https://github.com/vapoursynth/vapoursynth/releases/download/{0}/VapourSynth64-Portable-{0}.zip"
}

# Logging functions
function Write-Step {
    param([string]$Message)
    Write-Host "`n> $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "  [OK] $Message" -ForegroundColor Green
}

function Write-BuildWarning {
    param([string]$Message)
    Write-Host "  [WARN] $Message" -ForegroundColor Yellow
}

function Write-BuildError {
    param([string]$Message)
    Write-Host "  [ERROR] $Message" -ForegroundColor Red
}

# Utility functions
function Initialize-Directories {
    Write-Step "Initializing directories"

    $dirs = @(
        $script:Config.BuildDir,
        $script:Config.DistDir,
        (Join-Path $script:Config.DistDir "vapoursynth"),
        (Join-Path $script:Config.DistDir "python"),
        (Join-Path $script:Config.DistDir "plugins"),
        (Join-Path $script:Config.DistDir "scripts"),
        (Join-Path $script:Config.DistDir "presets")
    )

    foreach ($dir in $dirs) {
        if (-not (Test-Path $dir)) {
            New-Item -ItemType Directory -Path $dir -Force | Out-Null
            Write-Success "Created: $dir"
        }
    }
}

function Get-FileFromUrl {
    param(
        [string]$Url,
        [string]$Destination,
        [string]$Description
    )

    if (Test-Path $Destination) {
        Write-Success "$Description already downloaded"
        return $true
    }

    Write-Host "  Downloading: $Description..." -NoNewline
    try {
        Invoke-WebRequest -Uri $Url -OutFile $Destination -UseBasicParsing
        Write-Host " Done" -ForegroundColor Green
        return $true
    }
    catch {
        Write-Host " Failed" -ForegroundColor Red
        Write-BuildError $_.Exception.Message
        return $false
    }
}

function Expand-ArchivePortable {
    param(
        [string]$Path,
        [string]$Destination
    )

    if (Get-Command "7z" -ErrorAction SilentlyContinue) {
        & 7z x $Path -o"$Destination" -y | Out-Null
    }
    else {
        Expand-Archive -Path $Path -DestinationPath $Destination -Force
    }
}

# Component installation functions
function Install-Python {
    Write-Step "Installing Embedded Python $($script:Config.PythonVersion)"

    $pythonZip = Join-Path $script:Config.BuildDir "python-embed.zip"
    $pythonDir = Join-Path $script:Config.DistDir "python"
    $url = $script:Config.PythonUrl -f $script:Config.PythonVersion

    if (-not (Get-FileFromUrl -Url $url -Destination $pythonZip -Description "Python Embedded")) {
        throw "Failed to download Python"
    }

    Write-Host "  Extracting Python..." -NoNewline
    Expand-ArchivePortable -Path $pythonZip -Destination $pythonDir
    Write-Host " Done" -ForegroundColor Green

    # Enable pip by modifying python*._pth file
    $pthFile = Get-ChildItem -Path $pythonDir -Filter "python*._pth" | Select-Object -First 1
    if ($pthFile) {
        $content = Get-Content $pthFile.FullName
        $content = $content -replace "#import site", "import site"
        $content += "`n..\scripts"
        $content += "`n..\vapoursynth"
        Set-Content -Path $pthFile.FullName -Value $content
        Write-Success "Configured Python paths"
    }

    # Download get-pip.py
    $getPipUrl = "https://bootstrap.pypa.io/get-pip.py"
    $getPipPath = Join-Path $pythonDir "get-pip.py"
    Get-FileFromUrl -Url $getPipUrl -Destination $getPipPath -Description "pip installer"

    Write-Success "Python installation complete"
}

function Install-VapourSynth {
    Write-Step "Installing VapourSynth $($script:Config.VapourSynthVersion)"

    $vsZip = Join-Path $script:Config.BuildDir "vapoursynth-portable.zip"
    $vsDir = Join-Path $script:Config.DistDir "vapoursynth"
    $url = $script:Config.VapourSynthUrl -f $script:Config.VapourSynthVersion

    if (-not (Get-FileFromUrl -Url $url -Destination $vsZip -Description "VapourSynth Portable")) {
        throw "Failed to download VapourSynth"
    }

    Write-Host "  Extracting VapourSynth..." -NoNewline
    Expand-ArchivePortable -Path $vsZip -Destination $vsDir
    Write-Host " Done" -ForegroundColor Green

    Write-Success "VapourSynth installation complete"
}

function Install-Plugins {
    param([string]$PluginSet)

    $msg = "Installing plugins (" + $PluginSet + " set)"
    Write-Step $msg

    $configFile = Join-Path $script:Config.RootDir "plugins.json"
    if (-not (Test-Path $configFile)) {
        Write-BuildWarning "plugins.json not found, skipping plugin installation"
        return
    }

    $pluginConfig = Get-Content $configFile | ConvertFrom-Json
    $pluginDir = Join-Path $script:Config.DistDir "plugins"

    foreach ($plugin in $pluginConfig.plugins) {
        # Skip if not in selected plugin set
        if ($PluginSet -eq "minimal" -and $plugin.set -ne "minimal") { continue }
        if ($PluginSet -eq "standard" -and $plugin.set -eq "full") { continue }

        Write-Host "  Installing: $($plugin.name)..." -NoNewline

        try {
            # Get file extension from URL
            $urlPath = [System.Uri]::new($plugin.url).AbsolutePath
            $ext = [System.IO.Path]::GetExtension($urlPath)
            if (-not $ext) { $ext = ".zip" }
            $tempFile = Join-Path $script:Config.BuildDir "$($plugin.name)$ext"

            if (-not (Test-Path $tempFile)) {
                Invoke-WebRequest -Uri $plugin.url -OutFile $tempFile -UseBasicParsing
            }

            # Extract to temp and copy DLLs
            $tempExtract = Join-Path $script:Config.BuildDir "$($plugin.name)_extract"
            Expand-ArchivePortable -Path $tempFile -Destination $tempExtract

            # Find and copy DLL files
            Get-ChildItem -Path $tempExtract -Filter "*.dll" -Recurse |
                Where-Object { $_.Name -match "x64|64" -or $_.Directory.Name -match "x64|64" -or $_.Directory.Name -eq $plugin.name } |
                Copy-Item -Destination $pluginDir -Force

            Write-Host " Done" -ForegroundColor Green
        }
        catch {
            Write-Host " Failed" -ForegroundColor Red
            Write-BuildWarning "Could not install $($plugin.name): $($_.Exception.Message)"
        }
    }

    Write-Success "Plugin installation complete"
}

function Install-PythonPackages {
    Write-Step "Installing Python packages for VapourSynth"

    $pythonExe = Join-Path $script:Config.DistDir "python\python.exe"

    if (-not (Test-Path $pythonExe)) {
        Write-BuildWarning "Python not found, skipping package installation"
        return
    }

    # Install pip first
    $getPip = Join-Path $script:Config.DistDir "python\get-pip.py"
    if (Test-Path $getPip) {
        Write-Host "  Installing pip..." -NoNewline
        & $pythonExe $getPip --no-warn-script-location 2>&1 | Out-Null
        Write-Host " Done" -ForegroundColor Green
    }

    # Install VapourSynth from local wheel (bundled with portable distribution)
    $wheelDir = Join-Path $script:Config.DistDir "vapoursynth\wheel"
    $vsWheel = Get-ChildItem -Path $wheelDir -Filter "VapourSynth-*-cp312-*.whl" | Select-Object -First 1
    if ($vsWheel) {
        Write-Host "  Installing: vapoursynth (from wheel)..." -NoNewline
        & $pythonExe -m pip install $vsWheel.FullName --no-warn-script-location 2>&1 | Out-Null
        Write-Host " Done" -ForegroundColor Green
    }

    # Install additional Python packages from PyPI
    $packages = @(
        "vsutil",
        "havsfunc",
        "mvsfunc"
    )

    foreach ($package in $packages) {
        Write-Host "  Installing: $package..." -NoNewline
        try {
            & $pythonExe -m pip install $package --no-warn-script-location 2>&1 | Out-Null
            Write-Host " Done" -ForegroundColor Green
        }
        catch {
            Write-Host " Skipped" -ForegroundColor Yellow
        }
    }

    Write-Success "Python packages installed"
}

function New-Launcher {
    Write-Step "Creating launcher scripts"

    # Create batch launcher
    $batContent = @"
@echo off
setlocal EnableDelayedExpansion

:: VapourSynth Portable Launcher
set "SCRIPT_DIR=%~dp0"
set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

set "VS_PORTABLE=%SCRIPT_DIR%\dist"
set "PYTHON_HOME=%VS_PORTABLE%\python"
set "VS_HOME=%VS_PORTABLE%\vapoursynth"
set "VS_PLUGINS=%VS_PORTABLE%\plugins"
set "VS_SCRIPTS=%VS_PORTABLE%\scripts"

set "PATH=%PYTHON_HOME%;%PYTHON_HOME%\Scripts;%VS_HOME%;%PATH%"
set "PYTHONPATH=%VS_HOME%;%VS_SCRIPTS%;%PYTHONPATH%"
set "VAPOURSYNTH_PLUGINS=%VS_PLUGINS%"

if "%~1"=="" (
    echo.
    echo VapourSynth Portable Environment
    echo Python: %PYTHON_HOME%
    echo VapourSynth: %VS_HOME%
) else if "%~1"=="--shell" (
    "%PYTHON_HOME%\python.exe"
) else if "%~1"=="--info" (
    "%PYTHON_HOME%\python.exe" -c "import vapoursynth as vs; print('VapourSynth', vs.__version__)"
) else (
    "%PYTHON_HOME%\python.exe" "%~1" %2 %3 %4 %5 %6 %7 %8 %9
)
endlocal
"@

    $launcherPath = Join-Path $script:Config.RootDir "Launch-VapourSynth.bat"
    $batContent | Set-Content -Path $launcherPath -Encoding ASCII
    Write-Success "Created Launch-VapourSynth.bat"

    # Create PowerShell launcher
    $ps1Content = @'
# VapourSynth Portable - PowerShell Launcher
param([string]$Script, [switch]$Shell, [switch]$Info, [switch]$ListPlugins)

$ScriptDir = $PSScriptRoot
$env:VS_PORTABLE = Join-Path $ScriptDir "dist"
$env:PYTHON_HOME = Join-Path $env:VS_PORTABLE "python"
$env:VS_HOME = Join-Path $env:VS_PORTABLE "vapoursynth"
$env:VS_PLUGINS = Join-Path $env:VS_PORTABLE "plugins"
$env:VS_SCRIPTS = Join-Path $env:VS_PORTABLE "scripts"

$env:PATH = "$env:PYTHON_HOME;$env:VS_HOME;$env:PATH"
$env:PYTHONPATH = "$env:VS_HOME;$env:VS_SCRIPTS"
$env:VAPOURSYNTH_PLUGINS = $env:VS_PLUGINS

$python = Join-Path $env:PYTHON_HOME "python.exe"

if ($Info) {
    & $python -c "import vapoursynth as vs; print('VapourSynth', vs.__version__); print('Plugins:', len(list(vs.core.plugins())))"
}
elseif ($ListPlugins) {
    & $python -c "import vapoursynth as vs; [print(p.namespace) for p in vs.core.plugins()]"
}
elseif ($Shell) {
    & $python
}
elseif ($Script) {
    & $python $Script $args
}
else {
    Write-Host "VapourSynth Portable Environment"
    Write-Host "Python: $env:PYTHON_HOME"
    Write-Host "VapourSynth: $env:VS_HOME"
    Write-Host "Use -Info, -Shell, -ListPlugins, or provide a script path"
}
'@

    $launcherPs1Path = Join-Path $script:Config.RootDir "VSPortable.ps1"
    $ps1Content | Set-Content -Path $launcherPs1Path -Encoding UTF8
    Write-Success "Created VSPortable.ps1"
}

function New-VSCodeWorkspace {
    Write-Step "Creating VS Code workspace configuration"

    $vscodeDir = Join-Path $script:Config.RootDir ".vscode"
    if (-not (Test-Path $vscodeDir)) {
        New-Item -ItemType Directory -Path $vscodeDir | Out-Null
    }

    # settings.json
    $settings = @{
        "python.defaultInterpreterPath" = "`${workspaceFolder}/dist/python/python.exe"
        "python.analysis.extraPaths" = @(
            "`${workspaceFolder}/dist/vapoursynth",
            "`${workspaceFolder}/dist/scripts"
        )
        "files.associations" = @{
            "*.vpy" = "python"
        }
        "terminal.integrated.env.windows" = @{
            "PYTHONPATH" = "`${workspaceFolder}/dist/vapoursynth;`${workspaceFolder}/dist/scripts"
            "VAPOURSYNTH_PLUGINS" = "`${workspaceFolder}/dist/plugins"
        }
    }

    $settingsPath = Join-Path $vscodeDir "settings.json"
    $settings | ConvertTo-Json -Depth 5 | Set-Content -Path $settingsPath
    Write-Success "Created .vscode/settings.json"

    # launch.json
    $launch = @{
        "version" = "0.2.0"
        "configurations" = @(
            @{
                "name" = "Run VapourSynth Script"
                "type" = "python"
                "request" = "launch"
                "program" = "`${file}"
                "console" = "integratedTerminal"
                "python" = "`${workspaceFolder}/dist/python/python.exe"
                "env" = @{
                    "PYTHONPATH" = "`${workspaceFolder}/dist/vapoursynth;`${workspaceFolder}/dist/scripts"
                    "VAPOURSYNTH_PLUGINS" = "`${workspaceFolder}/dist/plugins"
                }
            }
        )
    }

    $launchPath = Join-Path $vscodeDir "launch.json"
    $launch | ConvertTo-Json -Depth 5 | Set-Content -Path $launchPath
    Write-Success "Created .vscode/launch.json"

    # extensions.json
    $extensions = @{
        "recommendations" = @(
            "ms-python.python",
            "ms-python.vscode-pylance",
            "ms-vscode.powershell"
        )
    }

    $extensionsPath = Join-Path $vscodeDir "extensions.json"
    $extensions | ConvertTo-Json -Depth 3 | Set-Content -Path $extensionsPath
    Write-Success "Created .vscode/extensions.json"
}

# Main build process
function Start-Build {
    Write-Host "`n========================================" -ForegroundColor Magenta
    Write-Host "   VapourSynth Portable Builder" -ForegroundColor Magenta
    Write-Host "========================================" -ForegroundColor Magenta

    if ($Clean) {
        Write-Step "Cleaning previous build"
        if (Test-Path $script:Config.BuildDir) {
            Remove-Item -Path $script:Config.BuildDir -Recurse -Force
            Write-Success "Removed build directory"
        }
        if (Test-Path $script:Config.DistDir) {
            Remove-Item -Path $script:Config.DistDir -Recurse -Force
            Write-Success "Removed dist directory"
        }
    }

    Initialize-Directories

    $componentList = if ($Components -eq "all") {
        @("python", "vapoursynth", "plugins", "packages", "launcher", "vscode")
    } else {
        $Components -split ","
    }

    foreach ($component in $componentList) {
        switch ($component.Trim().ToLower()) {
            "python" { Install-Python }
            "vapoursynth" { Install-VapourSynth }
            "plugins" { Install-Plugins -PluginSet $PluginSet }
            "packages" { Install-PythonPackages }
            "launcher" { New-Launcher }
            "vscode" { New-VSCodeWorkspace }
        }
    }

    Write-Host "`n========================================" -ForegroundColor Green
    Write-Host "   Build Complete!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "`nRun .\Launch-VapourSynth.bat to start`n" -ForegroundColor Cyan
}

# Entry point
Start-Build

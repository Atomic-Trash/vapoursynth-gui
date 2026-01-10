<#
.SYNOPSIS
    VapourSynth Portable - PowerShell Launcher

.DESCRIPTION
    Launches VapourSynth portable environment with proper path configuration.

.PARAMETER Script
    Path to a VapourSynth script (.vpy) to execute.

.PARAMETER Shell
    Start an interactive Python shell.

.PARAMETER Info
    Display VapourSynth version and plugin count.

.PARAMETER ListPlugins
    List all installed VapourSynth plugins.

.EXAMPLE
    .\VSPortable.ps1 -Info

.EXAMPLE
    .\VSPortable.ps1 -Script "myscript.vpy"
#>
param(
    [string]$Script,
    [switch]$Shell,
    [switch]$Info,
    [switch]$ListPlugins
)

# Import shared environment module
$modulePath = Join-Path $PSScriptRoot "Set-VSEnvironment.ps1"
if (Test-Path $modulePath) {
    . $modulePath
} else {
    Write-Error "Set-VSEnvironment.ps1 not found. Run Build-Portable.ps1 first."
    exit 1
}

# Determine dist path (parent of scripts/util)
$projectRoot = Split-Path (Split-Path $PSScriptRoot -Parent) -Parent
$distPath = Join-Path $projectRoot "dist"

# Validate and set up environment
if (-not (Test-VapourSynthEnvironment -DistPath $distPath)) {
    Write-Error "VapourSynth portable environment not properly set up. Run Build-Portable.ps1 first."
    exit 1
}

$paths = Set-VapourSynthEnvironment -DistPath $distPath -PassThru

if ($Info) {
    $info = Get-VapourSynthInfo -DistPath $distPath
    if ($info) {
        Write-Host "VapourSynth $($info.version)"
        Write-Host "API Version: $($info.api_version)"
        Write-Host "Plugins: $($info.plugin_count)"
    } else {
        Write-Error "Could not get VapourSynth info"
    }
}
elseif ($ListPlugins) {
    $plugins = Get-VapourSynthPlugins -DistPath $distPath
    foreach ($plugin in $plugins) {
        Write-Host $plugin
    }
}
elseif ($Shell) {
    & $paths.PythonExe
}
elseif ($Script) {
    & $paths.PythonExe $Script $args
}
else {
    Write-Host "VapourSynth Portable Environment"
    Write-Host "Python: $($paths.PythonHome)"
    Write-Host "VapourSynth: $($paths.VSHome)"
    Write-Host "Plugins: $($paths.Plugins)"
    Write-Host ""
    Write-Host "Usage: VSPortable.ps1 [-Info] [-Shell] [-ListPlugins] [-Script <path>]"
}

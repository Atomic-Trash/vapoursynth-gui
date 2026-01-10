<#
.SYNOPSIS
    Shared module for setting up VapourSynth portable environment.

.DESCRIPTION
    Provides common functions for configuring VapourSynth portable
    environment variables. Used by VSPortable.ps1 and other scripts.

.EXAMPLE
    . .\Set-VSEnvironment.ps1
    Set-VapourSynthEnvironment -DistPath "C:\VS\dist"

.EXAMPLE
    . .\Set-VSEnvironment.ps1
    $env = Get-VapourSynthPaths -DistPath "C:\VS\dist"
#>

function Get-VapourSynthPaths {
    <#
    .SYNOPSIS
        Gets VapourSynth portable paths without modifying environment.

    .PARAMETER DistPath
        Path to the dist directory containing python, vapoursynth, plugins folders.

    .OUTPUTS
        Hashtable with Python, VapourSynth, Plugins, and Scripts paths.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath
    )

    if (-not (Test-Path $DistPath)) {
        throw "Distribution path not found: $DistPath"
    }

    return @{
        DistPath    = $DistPath
        PythonHome  = Join-Path $DistPath "python"
        PythonExe   = Join-Path $DistPath "python\python.exe"
        VSHome      = Join-Path $DistPath "vapoursynth"
        Plugins     = Join-Path $DistPath "plugins"
        Scripts     = Join-Path $DistPath "scripts"
        Presets     = Join-Path $DistPath "presets"
    }
}

function Set-VapourSynthEnvironment {
    <#
    .SYNOPSIS
        Configures environment variables for VapourSynth portable.

    .PARAMETER DistPath
        Path to the dist directory containing python, vapoursynth, plugins folders.

    .PARAMETER PassThru
        Return the paths hashtable after setting environment.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath,
        [switch]$PassThru
    )

    $paths = Get-VapourSynthPaths -DistPath $DistPath

    # Set environment variables
    $env:VS_PORTABLE = $paths.DistPath
    $env:PYTHON_HOME = $paths.PythonHome
    $env:VS_HOME = $paths.VSHome
    $env:VS_PLUGINS = $paths.Plugins
    $env:VS_SCRIPTS = $paths.Scripts
    $env:VAPOURSYNTH_PLUGINS = $paths.Plugins

    # Update PATH
    $pathEntries = @(
        $paths.PythonHome,
        (Join-Path $paths.PythonHome "Scripts"),
        $paths.VSHome
    )
    $env:PATH = ($pathEntries -join ";") + ";$env:PATH"

    # Set PYTHONPATH
    $env:PYTHONPATH = "$($paths.VSHome);$($paths.Scripts)"

    if ($PassThru) {
        return $paths
    }
}

function Test-VapourSynthEnvironment {
    <#
    .SYNOPSIS
        Validates that VapourSynth environment is properly configured.

    .PARAMETER DistPath
        Path to the dist directory to validate.

    .OUTPUTS
        Boolean indicating if environment is valid.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath
    )

    $paths = Get-VapourSynthPaths -DistPath $DistPath
    $issues = @()

    if (-not (Test-Path $paths.PythonExe)) {
        $issues += "Python not found at: $($paths.PythonExe)"
    }

    if (-not (Test-Path $paths.VSHome)) {
        $issues += "VapourSynth not found at: $($paths.VSHome)"
    }

    if (-not (Test-Path $paths.Plugins)) {
        $issues += "Plugins directory not found at: $($paths.Plugins)"
    }

    if ($issues.Count -gt 0) {
        foreach ($issue in $issues) {
            Write-Warning $issue
        }
        return $false
    }

    return $true
}

function Get-VapourSynthInfo {
    <#
    .SYNOPSIS
        Gets VapourSynth version and plugin information.

    .PARAMETER DistPath
        Path to the dist directory.

    .OUTPUTS
        Object with VapourSynth version and plugin count.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath
    )

    $paths = Get-VapourSynthPaths -DistPath $DistPath

    if (-not (Test-Path $paths.PythonExe)) {
        return $null
    }

    Set-VapourSynthEnvironment -DistPath $DistPath

    try {
        $output = & $paths.PythonExe -c @"
import vapoursynth as vs
import json
info = {
    'version': vs.__version__,
    'api_version': vs.core.version_number(),
    'plugin_count': len(list(vs.core.plugins()))
}
print(json.dumps(info))
"@ 2>$null

        return $output | ConvertFrom-Json
    }
    catch {
        return $null
    }
}

function Get-VapourSynthPlugins {
    <#
    .SYNOPSIS
        Gets list of installed VapourSynth plugins.

    .PARAMETER DistPath
        Path to the dist directory.

    .OUTPUTS
        Array of plugin namespace names.
    #>
    param(
        [Parameter(Mandatory = $true)]
        [string]$DistPath
    )

    $paths = Get-VapourSynthPaths -DistPath $DistPath

    if (-not (Test-Path $paths.PythonExe)) {
        return @()
    }

    Set-VapourSynthEnvironment -DistPath $DistPath

    try {
        $output = & $paths.PythonExe -c @"
import vapoursynth as vs
for p in vs.core.plugins():
    print(p.namespace)
"@ 2>$null

        return $output -split "`n" | Where-Object { $_ }
    }
    catch {
        return @()
    }
}

# Export functions when used as module
Export-ModuleMember -Function @(
    'Get-VapourSynthPaths',
    'Set-VapourSynthEnvironment',
    'Test-VapourSynthEnvironment',
    'Get-VapourSynthInfo',
    'Get-VapourSynthPlugins'
)

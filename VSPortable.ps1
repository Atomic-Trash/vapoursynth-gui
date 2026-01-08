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

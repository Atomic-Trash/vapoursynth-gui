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

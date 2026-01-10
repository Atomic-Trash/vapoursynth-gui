@echo off
setlocal EnableDelayedExpansion

:: VapourSynth Portable Launcher
:: For advanced options, use VSPortable.ps1

set "SCRIPT_DIR=%~dp0"
set "SCRIPT_DIR=%SCRIPT_DIR:~0,-1%"

:: Navigate to project root (parent of scripts\util)
for %%i in ("%SCRIPT_DIR%") do set "UTIL_DIR=%%~dpi"
for %%i in ("%UTIL_DIR:~0,-1%") do set "SCRIPTS_DIR=%%~dpi"
set "PROJECT_ROOT=%SCRIPTS_DIR:~0,-1%"

set "VS_PORTABLE=%PROJECT_ROOT%\dist"
set "PYTHON_HOME=%VS_PORTABLE%\python"
set "VS_HOME=%VS_PORTABLE%\vapoursynth"
set "VS_PLUGINS=%VS_PORTABLE%\plugins"
set "VS_SCRIPTS=%VS_PORTABLE%\scripts"

:: Validate environment
if not exist "%PYTHON_HOME%\python.exe" (
    echo ERROR: Python not found at %PYTHON_HOME%
    echo Run Build-Portable.ps1 first to set up the environment.
    exit /b 1
)

:: Set environment variables
set "PATH=%PYTHON_HOME%;%PYTHON_HOME%\Scripts;%VS_HOME%;%PATH%"
set "PYTHONPATH=%VS_HOME%;%VS_SCRIPTS%;%PYTHONPATH%"
set "VAPOURSYNTH_PLUGINS=%VS_PLUGINS%"

if "%~1"=="" (
    echo.
    echo VapourSynth Portable Environment
    echo ================================
    echo Python:      %PYTHON_HOME%
    echo VapourSynth: %VS_HOME%
    echo Plugins:     %VS_PLUGINS%
    echo.
    echo Usage: Launch-VapourSynth.bat [options] [script.vpy]
    echo.
    echo Options:
    echo   --shell      Start interactive Python shell
    echo   --info       Show VapourSynth version info
    echo   --plugins    List installed plugins
    echo   --help       Show this help
    echo.
    echo For advanced options, use: powershell -File VSPortable.ps1
) else if "%~1"=="--shell" (
    "%PYTHON_HOME%\python.exe"
) else if "%~1"=="--info" (
    "%PYTHON_HOME%\python.exe" -c "import vapoursynth as vs; print('VapourSynth', vs.__version__); print('Plugins:', len(list(vs.core.plugins())))"
) else if "%~1"=="--plugins" (
    "%PYTHON_HOME%\python.exe" -c "import vapoursynth as vs; [print(p.namespace) for p in vs.core.plugins()]"
) else if "%~1"=="--help" (
    call :show_help
) else (
    "%PYTHON_HOME%\python.exe" "%~1" %2 %3 %4 %5 %6 %7 %8 %9
)
endlocal
goto :eof

:show_help
echo.
echo VapourSynth Portable Launcher
echo ================================
echo.
echo Usage: Launch-VapourSynth.bat [options] [script.vpy]
echo.
echo Options:
echo   --shell      Start interactive Python shell
echo   --info       Show VapourSynth version and plugin count
echo   --plugins    List all installed plugins
echo   --help       Show this help message
echo.
echo Examples:
echo   Launch-VapourSynth.bat --info
echo   Launch-VapourSynth.bat myscript.vpy
echo   Launch-VapourSynth.bat --shell
echo.
goto :eof

@echo off
title Touhou Web Arena - Web Exporter
cd /d %~dp0
echo Exporting Web Release...
if not exist build\web mkdir build\web
"%USERPROFILE%\Desktop\Godot_v4.7.2-stable_win64.exe" --headless --export-release "TouhouWebArena" "build/web/index.html"
if %ERRORLEVEL% equ 0 (
    echo.
    echo ========================================================
    echo  Export completed successfully to build/web/!
    echo ========================================================
) else (
    echo.
    echo [ERROR] Export failed with error code %ERRORLEVEL%
)
pause

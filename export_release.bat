@echo off
title Touhou Web Arena - Release Exporter
cd /d %~dp0

if "%~1"=="" (
    echo Usage: export_release.bat ^<version^>
    echo   e.g. export_release.bat v0.2
    exit /b 1
)

set VERSION=%~1
set TAGNAME=release-%VERSION%
set GODOT="%USERPROFILE%\Desktop\Godot_v4.7.2-stable_win64.exe"

git diff --quiet --exit-code
if %ERRORLEVEL% neq 0 goto dirty
git diff --cached --quiet --exit-code
if %ERRORLEVEL% neq 0 goto dirty
for /f %%i in ('git status --porcelain') do goto dirty
goto clean

:dirty
echo [ERROR] Working tree has uncommitted changes.
echo Commit or stash them first so the tag ^(and the build^) matches what you expect.
pause
exit /b 1

:clean
echo Tagging current commit as %TAGNAME%...
git tag %TAGNAME%
if %ERRORLEVEL% neq 0 (
    echo [ERROR] Tag creation failed - it may already exist.
    pause
    exit /b 1
)

echo.
echo Exporting Web build...
if not exist build\web mkdir build\web
%GODOT% --headless --export-release "TouhouWebArena" "build/web/index.html"

echo.
echo Exporting Windows Desktop build...
if not exist build\windows mkdir build\windows
%GODOT% --headless --export-release "Windows Desktop" "build/windows/TouhouWebArena.exe"

echo.
echo Packaging Windows build for GitHub release...
set ZIPFILE=build\%TAGNAME%-windows.zip
if exist "%ZIPFILE%" del "%ZIPFILE%"
powershell -NoProfile -Command "Compress-Archive -Path 'build\windows\*' -DestinationPath '%ZIPFILE%'"

echo.
echo Pushing tag %TAGNAME% and creating draft GitHub release (desktop build only)...
git push origin %TAGNAME%
gh release create %TAGNAME% "%ZIPFILE%" --draft --title "Touhou Web Arena %VERSION%" --notes "Desktop client build for %TAGNAME%. Web build for this version is deployed separately to itch.io."
if %ERRORLEVEL% neq 0 (
    echo [WARNING] GitHub release step failed - check that 'gh' is installed and 'gh auth login' has been run.
)

echo.
echo ========================================================
echo  Release %VERSION% exported (tag: %TAGNAME%)
echo  Web:     build/web/
echo  Windows: build/windows/  ^(zipped and attached to a DRAFT GitHub release^)
echo.
echo  Go to the repo's Releases page on GitHub to review and Publish it.
echo ========================================================
pause

@echo off
REM ============================================================
REM  Touhou Web Arena - restore onto a fresh Windows install
REM  Run this AFTER installing: Godot 4.7.2, VS Code + the Claude
REM  Code extension, and Antigravity IDE (at least once each, so
REM  they create their folders). Then run this script.
REM ============================================================
setlocal

set "SRC=%~1"
if "%SRC%"=="" set /p "SRC=Backup folder to restore FROM (e.g. E:\TWA_Backup): "
if "%SRC%"=="" goto :nosrc
if not exist "%SRC%\BACKUP_INFO.txt" goto :badsrc

echo.
echo ============================================================
echo  Restoring from: %SRC%
echo ============================================================
echo.
echo  Current Windows user is: %USERNAME%
echo  The backup expects the project to land at:
echo      %USERPROFILE%\Desktop\TouhouWebArena
echo.
REM The backup records which user it was taken from. Compare against it
REM rather than hardcoding a name.
set "ORIGUSER="
for /f "tokens=1,* delims=:" %%U in ('findstr /b /c:"Source user name:" "%SRC%\BACKUP_INFO.txt"') do set "ORIGUSER=%%V"
for /f "tokens=* delims= " %%W in ("%ORIGUSER%") do set "ORIGUSER=%%W"
if defined ORIGUSER if /I not "%USERNAME%"=="%ORIGUSER%" (
  echo  ** YOUR USERNAME IS NOT "%ORIGUSER%". **
  echo  Claude and Gemini key their saved conversations to the old
  echo  absolute path. Close this, make a user named "%ORIGUSER%", and
  echo  restore from there - or read MACHINE_RESTORE.md first.
  echo.
)
echo  This will OVERWRITE existing Claude/Gemini history on this
echo  machine. Press Ctrl+C now if that is not what you want.
echo.
pause

echo.
echo [1/6] %TIME:~0,8%  Project folder...
robocopy "%SRC%\project\TouhouWebArena" "%USERPROFILE%\Desktop\TouhouWebArena" /E /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: project restore reported errors **

echo [2/6] %TIME:~0,8%  Claude Code history, memory and settings...
robocopy "%SRC%\claude\dot-claude" "%USERPROFILE%\.claude" /E /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: .claude restore reported errors **
copy /Y "%SRC%\claude\claude.json" "%USERPROFILE%\.claude.json" >nul

echo [3/6] %TIME:~0,8%  Gemini / Antigravity conversations...
robocopy "%SRC%\gemini\antigravity" "%USERPROFILE%\.gemini\antigravity" /E /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: antigravity restore reported errors **
robocopy "%SRC%\gemini\config" "%USERPROFILE%\.gemini\config" /E /R:1 /W:1 /NFL /NP /NJH

echo [4/6] %TIME:~0,8%  IDE settings...
robocopy "%SRC%\gemini\AntigravityIDE_User" "%APPDATA%\Antigravity IDE\User" /E /R:1 /W:1 /NFL /NP /NJH
robocopy "%SRC%\gemini\antigravity_extensions" "%USERPROFILE%\.antigravity-ide\extensions" /E /R:1 /W:1 /NFL /NP /NJH
robocopy "%SRC%\vscode\User" "%APPDATA%\Code\User" /E /R:1 /W:1 /NFL /NP /NJH

echo [5/6] %TIME:~0,8%  Godot editor settings...
copy /Y "%SRC%\godot\editor_settings-4.7.tres" "%APPDATA%\Godot\" >nul 2>&1
copy /Y "%SRC%\godot\projects.cfg" "%APPDATA%\Godot\" >nul 2>&1
robocopy "%SRC%\godot\app_userdata" "%APPDATA%\Godot\app_userdata" /E /R:1 /W:1 /NFL /NP /NJH

echo [6/6] %TIME:~0,8%  Gemini codebase index, if it was backed up...
if exist "%SRC%\gemini\antigravity\brain" (
  echo    Restored with step 3.
) else (
  echo    Not in the backup. Gemini will rebuild it on first use.
)

echo.
echo ============================================================
echo  RESTORE COMPLETE - a few things are still left to do:
echo.
echo   1. Godot: install 4.7.2, then Editor ^> Manage Export
echo      Templates ^> Download (the 513 MB templates were not
echo      backed up on purpose).
echo   2. Node server deps: open a terminal in
echo      Desktop\TouhouWebArena\server and run:  npm install
echo   3. Log back in to Claude Code and to Antigravity
echo      (passwords/tokens were deliberately NOT backed up).
echo   4. Side tooling: install Python 3.11 then run
echo         pip install opencv-python numpy pillow
echo      and install Node.js 24.x for the signaling server.
echo   5. Open the project in Godot once so it rebuilds .godot\
echo      (this takes a few minutes the first time).
echo   6. Press F5 in Godot to confirm the game still runs.
echo ============================================================
echo.
pause
exit /b 0

:nosrc
echo No backup folder given. Nothing was restored.
pause
exit /b 1

:badsrc
echo "%SRC%" does not look like a backup made by backup_machine.bat
echo (BACKUP_INFO.txt is missing). Nothing was restored.
pause
exit /b 1

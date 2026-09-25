@echo off
REM ============================================================
REM  Touhou Web Arena - full machine backup
REM  Copies the project + all Claude and Gemini/Antigravity
REM  conversation history to a destination folder (external drive).
REM  Safe to re-run: it updates the backup instead of duplicating.
REM ============================================================
setlocal

set "DEST=%~1"
if "%DEST%"=="" set /p "DEST=Backup destination folder (e.g. E:\TWA_Backup): "
if "%DEST%"=="" goto :nodest

set "INCLUDE_BRAIN=%~2"

echo.
echo ============================================================
echo  Backing up to: %DEST%
echo ============================================================
echo.
echo  IMPORTANT: before you format, also run this in the project:
echo      git push origin main
echo  (there may be commits that only exist on this machine)
echo.
pause

if not exist "%DEST%" mkdir "%DEST%"

echo.
echo [1/7] %TIME:~0,8%  Project folder (source, assets, docs, scratch)...
robocopy "%USERPROFILE%\Desktop\TouhouWebArena" "%DEST%\project\TouhouWebArena" /E /XD .godot build node_modules /XF *.tmp /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: project copy reported errors **

echo [2/7] %TIME:~0,8%  Claude Code history, memory and settings...
if not exist "%DEST%\claude" mkdir "%DEST%\claude"
robocopy "%USERPROFILE%\.claude" "%DEST%\claude\dot-claude" /E /XD cache paste-cache shell-snapshots telemetry downloads session-env /XF .credentials.json /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: .claude copy reported errors **
copy /Y "%USERPROFILE%\.claude.json" "%DEST%\claude\claude.json" >nul

echo [3/7] %TIME:~0,8%  Gemini / Antigravity conversations...
robocopy "%USERPROFILE%\.gemini\antigravity" "%DEST%\gemini\antigravity" /E /XD brain bin builtin crashes log /R:1 /W:1 /NFL /NP /NJH
if %ERRORLEVEL% GEQ 8 echo    ** WARNING: antigravity copy reported errors **
robocopy "%USERPROFILE%\.gemini\config" "%DEST%\gemini\config" /E /R:1 /W:1 /NFL /NP /NJH

echo [4/7] %TIME:~0,8%  Antigravity IDE settings...
robocopy "%APPDATA%\Antigravity IDE\User" "%DEST%\gemini\AntigravityIDE_User" /E /XD CachedData GPUCache /R:1 /W:1 /NFL /NP /NJH
robocopy "%USERPROFILE%\.antigravity-ide\extensions" "%DEST%\gemini\antigravity_extensions" /E /R:1 /W:1 /NFL /NP /NJH

echo [5/7] %TIME:~0,8%  VS Code settings and extension state...
robocopy "%APPDATA%\Code\User" "%DEST%\vscode\User" /E /XD CachedData /R:1 /W:1 /NFL /NP /NJH

echo [6/7] %TIME:~0,8%  Godot editor settings...
if not exist "%DEST%\godot" mkdir "%DEST%\godot"
copy /Y "%APPDATA%\Godot\editor_settings-4.7.tres" "%DEST%\godot\" >nul 2>&1
copy /Y "%APPDATA%\Godot\projects.cfg" "%DEST%\godot\" >nul 2>&1
robocopy "%APPDATA%\Godot\app_userdata" "%DEST%\godot\app_userdata" /E /R:1 /W:1 /NFL /NP /NJH

echo [7/7] %TIME:~0,8%  Gemini codebase index ("brain")...
if /I "%INCLUDE_BRAIN%"=="brain" (
  echo    Including brain - this is another ~4 GB...
  robocopy "%USERPROFILE%\.gemini\antigravity\brain" "%DEST%\gemini\antigravity\brain" /E /R:1 /W:1 /NFL /NP /NJH
) else (
  echo    Skipped ^(Gemini rebuilds it automatically^). Pass "brain" as the
  echo    second argument if you want it copied anyway.
)

echo.
echo Writing manifest...
> "%DEST%\BACKUP_INFO.txt" echo Touhou Web Arena machine backup
>> "%DEST%\BACKUP_INFO.txt" echo Created: %DATE% %TIME%
>> "%DEST%\BACKUP_INFO.txt" echo Source user profile: %USERPROFILE%
>> "%DEST%\BACKUP_INFO.txt" echo Source user name: %USERNAME%
>> "%DEST%\BACKUP_INFO.txt" echo.
>> "%DEST%\BACKUP_INFO.txt" echo Restore with restore_machine.bat, or follow MACHINE_RESTORE.md
>> "%DEST%\BACKUP_INFO.txt" echo in project\TouhouWebArena\.
>> "%DEST%\BACKUP_INFO.txt" echo.
>> "%DEST%\BACKUP_INFO.txt" echo The project MUST be restored to the exact same path:
>> "%DEST%\BACKUP_INFO.txt" echo     %USERPROFILE%\Desktop\TouhouWebArena
copy /Y "%~dp0MACHINE_RESTORE.md" "%DEST%\" >nul 2>&1
copy /Y "%~dp0restore_machine.bat" "%DEST%\" >nul 2>&1

echo.
echo ============================================================
echo  VERIFYING - counting what actually landed on the drive
echo ============================================================
set "FAIL=0"
REM Claude names its project folder after the project's full path, with ":"
REM and "\" replaced by "-". Derive it rather than hardcoding a user name.
set "CLAUDEKEY=%USERPROFILE%\Desktop\TouhouWebArena"
set "CLAUDEKEY=%CLAUDEKEY::=-%"
set "CLAUDEKEY=%CLAUDEKEY:\=-%"
call :checkdir "Project files"        "%DEST%\project\TouhouWebArena"
call :checkdir "Claude transcripts"   "%DEST%\claude\dot-claude\projects"
call :checkdir "Claude memory"        "%DEST%\claude\dot-claude\projects\%CLAUDEKEY%\memory"
call :checkdir "Gemini conversations" "%DEST%\gemini\antigravity\conversations"
call :checkdir "VS Code settings"     "%DEST%\vscode\User"

echo.
echo ============================================================
if "%FAIL%"=="0" (
  echo  BACKUP COMPLETE AND VERIFIED
  echo  Location: %DEST%
  echo.
  echo  Every section has files in it. You are safe to format.
) else (
  echo  ** BACKUP INCOMPLETE - %FAIL% SECTION^(S^) EMPTY OR MISSING **
  echo  Do NOT format yet. Scroll up to see which section failed.
)
echo ============================================================
echo.
pause
exit /b 0

:checkdir
set "LABEL=%~1"
set "TARGET=%~2"
set "N=0"
if exist "%TARGET%" for /f %%C in ('dir /s /b /a-d "%TARGET%" 2^>nul ^| find /c /v ""') do set "N=%%C"
if "%N%"=="0" (
  echo    [ FAIL ] %LABEL%: 0 files
  set /a FAIL+=1
) else (
  echo    [  ok  ] %LABEL%: %N% files
)
exit /b 0

:nodest
echo No destination given. Nothing was copied.
pause
exit /b 1

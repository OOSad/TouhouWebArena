@echo off
echo Repairing Git repository index...
del /f /q .git\index.lock 2>nul
del /f /q .git\index 2>nul

for /d %%D in ("%LOCALAPPDATA%\GitHubDesktop\app-*") do (
    if exist "%%D\resources\app\git\cmd\git.exe" (
        set "GIT_EXE=%%D\resources\app\git\cmd\git.exe"
    )
)

if defined GIT_EXE (
    "%GIT_EXE%" reset
    echo.
    echo Git repository index repaired successfully!
) else (
    git reset
    echo.
    echo Git repository index repaired using system Git!
)
pause
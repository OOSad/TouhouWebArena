@echo off
title Touhou Web Arena - Automated Test Runner
cd /d %~dp0

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0tests\run_tests.ps1"

set TEST_EXIT_CODE=%ERRORLEVEL%
echo.
if %TEST_EXIT_CODE% equ 0 (
    echo [SUCCESS] All unit tests completed without errors.
) else (
    echo [ERROR] Some unit tests failed. See details above.
)
exit /b %TEST_EXIT_CODE%

@echo off
title Touhou Web Arena - Local Testing Server
cd /d "%~dp0"

echo ===================================================================
echo               TOUHOU WEB ARENA - LOCAL TESTING SERVER
echo ===================================================================
echo.
echo  [1/2] Signaling Server (WebRTC switchboard) : ws://localhost:8910
echo  [2/2] Game Web Server   (HTML5 Web build)   : http://localhost:3000
echo  Press Ctrl+C in this window at any time to shut down the servers.
echo ===================================================================
echo.

node server/run_local_test.js

pause


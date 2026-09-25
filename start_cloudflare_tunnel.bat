@echo off
title Touhou Web Arena - Cloudflare Public Tunnel
cd /d %~dp0

echo ===================================================================
echo               TOUHOU WEB ARENA - CLOUDFLARE PUBLIC TUNNEL
echo ===================================================================
echo.
echo Starting high-speed tunnel to http://localhost:3000 ...
echo Look for the https://....trycloudflare.com link below!
echo Share that link with your friends to play online.
echo.
echo Press Ctrl+C in this window at any time to close the tunnel.
echo ===================================================================
echo.

server\cloudflared.exe tunnel --edge-ip-version 4 --url http://localhost:3000

pause

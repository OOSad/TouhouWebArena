const http = require('http');
const fs = require('fs');
const path = require('path');

const { attachSignaling } = require('./signaling_server.js');

// 2. Start Local Game Web Server (HTTP on 3000)
const HTTP_PORT = 3000;
const WEB_ROOT = path.join(__dirname, '..', 'build', 'web');

const MIME_TYPES = {
  '.html': 'text/html',
  '.js': 'text/javascript',
  '.wasm': 'application/wasm',
  '.pck': 'application/octet-stream',
  '.png': 'image/png',
  '.svg': 'image/svg+xml',
  '.json': 'application/json',
  '.ico': 'image/x-icon'
};

const server = http.createServer((req, res) => {
  // Required security headers for Godot 4 Web exports
  res.setHeader('Cross-Origin-Opener-Policy', 'same-origin');
  res.setHeader('Cross-Origin-Embedder-Policy', 'require-corp');
  // Disable HTTP cache so web rebuilds are immediately picked up on page reload
  res.setHeader('Cache-Control', 'no-store, no-cache, must-revalidate, proxy-revalidate');
  res.setHeader('Pragma', 'no-cache');
  res.setHeader('Expires', '0');

  const cleanUrl = req.url.split('?')[0];
  const relativePath = cleanUrl === '/' ? 'index.html' : cleanUrl.replace(/^\//, '');
  const filePath = path.join(WEB_ROOT, relativePath);

  fs.readFile(filePath, (err, data) => {
    if (err) {
      res.writeHead(404, { 'Content-Type': 'text/plain' });
      res.end(`404 Not Found: ${cleanUrl}`);
      return;
    }
    const ext = path.extname(filePath).toLowerCase();
    res.writeHead(200, { 'Content-Type': MIME_TYPES[ext] || 'application/octet-stream' });
    res.end(data);
  });
});

// Attach WebSocket signaling server directly to this HTTP server (port 3000)
attachSignaling(server);

// Also listen on standalone port 8910 for desktop Godot clients / F5 testing
attachSignaling(8910);

server.on('error', (err) => {
  if (err.code === 'EADDRINUSE') {
    console.warn(`[Web Server] Notice: Port ${HTTP_PORT} is already in use by another instance.`);
  } else {
    console.error(`[Web Server] Error:`, err.message);
  }
});

server.listen(HTTP_PORT, () => {
  console.log(`[Web Server] Game running at: http://localhost:${HTTP_PORT}/`);
  console.log(`[Web Server] Dual-signaling active: port ${HTTP_PORT} (WSS single-tunnel ready) & port 8910!`);
  console.log(`[Web Server] Ready! Open http://localhost:${HTTP_PORT}/ in two browser tabs or share your tunnel URL.`);
});


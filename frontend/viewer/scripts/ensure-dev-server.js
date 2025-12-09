#!/usr/bin/env node
/**
 * Ensure dev server is running on port 5173
 * Starts it if not already running, then waits for it to be ready
 */

const http = require('http');
const { spawn } = require('child_process');
const path = require('path');

const PORT = 5173;
const MAX_WAIT = 30000; // 30 seconds
const CHECK_INTERVAL = 500; // 500ms

function isServerRunning() {
  return new Promise((resolve) => {
    const req = http.get(`http://localhost:${PORT}`, { timeout: 2000 }, () => {
      resolve(true);
    });
    req.on('error', () => resolve(false));
    req.on('timeout', () => resolve(false));
  });
}

async function waitForServer(maxWait = MAX_WAIT) {
  const startTime = Date.now();
  while (Date.now() - startTime < maxWait) {
    if (await isServerRunning()) {
      console.log(`✓ Dev server ready on http://localhost:${PORT}`);
      return true;
    }
    await new Promise(resolve => setTimeout(resolve, CHECK_INTERVAL));
  }
  return false;
}

async function startServer() {
  console.log(`Starting dev server on port ${PORT}...`);
  
  // Determine OS-specific command
  const isWindows = process.platform === 'win32';
  const cmd = isWindows ? 'pnpm.cmd' : 'pnpm';
  const args = ['run', 'dev'];
  
  const server = spawn(cmd, args, {
    cwd: __dirname + '/..',
    stdio: 'inherit',
    shell: isWindows,
  });

  // Let it run in background
  server.unref();
  
  // Wait for it to be ready
  const ready = await waitForServer();
  if (!ready) {
    console.error(`✗ Dev server failed to start within ${MAX_WAIT / 1000}s`);
    process.exit(1);
  }
}

async function main() {
  const running = await isServerRunning();
  
  if (running) {
    console.log(`✓ Dev server already running on http://localhost:${PORT}`);
  } else {
    await startServer();
  }
}

main().catch(err => {
  console.error('Error:', err);
  process.exit(1);
});

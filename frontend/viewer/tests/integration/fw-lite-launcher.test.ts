import {existsSync} from 'node:fs';
import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {FwLiteLauncher} from '../e2e/helpers/fw-lite-launcher';
import {testConfig} from '../e2e/config';

// Hard-fail if the binary is missing. Running this suite at all means you've opted in
// (via `pnpm test:integration` or the e2e CI job). The default `pnpm test` excludes
// the integration project so unrelated workflows aren't burdened with building the binary.
const fwLiteBinaryPath = testConfig.fwLite.binaryPath;
if (!existsSync(fwLiteBinaryPath)) {
  throw new Error(
    `FW Lite binary not found at ${fwLiteBinaryPath}. ` +
    `Build it (e.g. \`task -d frontend/viewer test:e2e-setup\`) ` +
    `or set FW_LITE_BINARY_PATH to a built binary.`
  );
}

describe('FwLiteLauncher', () => {
  let launcher: FwLiteLauncher;
  beforeEach(() => { launcher = new FwLiteLauncher(); });
  afterEach(async () => { if (launcher.isRunning()) await launcher.shutdown(); });

  it('reports not running before launch', () => {
    expect(launcher.isRunning()).toBe(false);
    expect(() => launcher.getBaseUrl()).toThrow('FW Lite is not running');
  });

  it('shutdown is a no-op when not running', async () => {
    await expect(launcher.shutdown()).resolves.not.toThrow();
  });

  it('rejects when the binary does not exist', async () => {
    await expect(launcher.launch({
      binaryPath: '/nonexistent/path/to/fw-lite',
      serverUrl: 'http://localhost:5137',
      timeout: 1000,
    })).rejects.toThrow('FW Lite binary not found or not executable');
  });

  it('launches and shuts down the real binary', async () => {
    await launcher.launch({
      binaryPath: fwLiteBinaryPath,
      serverUrl: 'http://localhost:5137',
      port: 5555,
      timeout: 30_000,
    });
    expect(launcher.isRunning()).toBe(true);
    expect(launcher.getBaseUrl()).toBe('http://localhost:5555');

    const response = await fetch(`${launcher.getBaseUrl()}/health`);
    expect(response.ok).toBe(true);

    await launcher.shutdown();
    expect(launcher.isRunning()).toBe(false);
  }, 60_000);

  it('rejects a second launch while already running', async () => {
    await launcher.launch({
      binaryPath: fwLiteBinaryPath,
      serverUrl: 'http://localhost:5137',
      port: 5556,
      timeout: 30_000,
    });
    await expect(launcher.launch({
      binaryPath: fwLiteBinaryPath,
      serverUrl: 'http://localhost:5137',
      port: 5557,
    })).rejects.toThrow('FW Lite is already running');
  }, 60_000);
});

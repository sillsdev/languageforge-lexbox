import {afterEach, beforeEach, describe, expect, it} from 'vitest';
import {existsSync} from 'node:fs';

import {FwLiteLauncher, type LaunchConfig} from '../e2e/helpers/fw-lite-launcher';
import {fwLiteBinaryPath} from '../e2e/config';

// Hard-fail rather than skip: running this suite is an explicit opt-in (`pnpm test:launcher` / the e2e CI job),
// so a missing binary is a real error, not a reason to silently pass.
if (!existsSync(fwLiteBinaryPath)) {
  throw new Error(
    `FW Lite binary not found at ${fwLiteBinaryPath}. ` +
    `Build it (e.g. \`task -d frontend/viewer test:build-launcher\`) ` +
    `or set FW_LITE_BINARY_PATH to a built binary.`
  );
}

const DUMMY_SERVER_URL = 'http://localhost:5137';

function launchConfig(overrides: Partial<LaunchConfig> = {}): LaunchConfig {
  return {
    binaryPath: fwLiteBinaryPath,
    serverUrl: DUMMY_SERVER_URL,
    timeout: 30_000,
    ...overrides,
  };
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
    await expect(launcher.launch(launchConfig({
      binaryPath: '/nonexistent/path/to/fw-lite',
      timeout: 1000,
    }))).rejects.toThrow('FW Lite binary not found or not executable');
  });

  it('launches and shuts down the real binary', async () => {
    await launcher.launch(launchConfig({port: 5555}));
    expect(launcher.isRunning()).toBe(true);
    expect(launcher.getBaseUrl()).toBe('http://localhost:5555');

    const response = await fetch(`${launcher.getBaseUrl()}/health`);
    expect(response.ok).toBe(true);

    await launcher.shutdown();
    expect(launcher.isRunning()).toBe(false);
  }, 60_000);

  it('rejects a second launch while already running', async () => {
    await launcher.launch(launchConfig({port: 5556}));
    await expect(launcher.launch(launchConfig({port: 5557})))
      .rejects.toThrow('FW Lite is already running');
  }, 60_000);
});

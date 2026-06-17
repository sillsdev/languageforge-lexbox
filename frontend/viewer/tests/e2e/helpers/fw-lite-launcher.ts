import {spawn, type ChildProcess} from 'node:child_process';
import {access, constants} from 'node:fs/promises';
import {createServer, type AddressInfo} from 'node:net';
import {platform} from 'node:os';

export interface LaunchConfig {
  binaryPath: string;
  serverUrl: string;
  port?: number;
  timeout?: number;
  logFile?: string;
}

const DEFAULT_LAUNCH_TIMEOUT_MS = 30_000;
const SHUTDOWN_TIMEOUT_MS = 10_000;
const HEALTH_CHECK_INTERVAL_MS = 1_000;
const HEALTH_CHECK_REQUEST_TIMEOUT_MS = 5_000;
const READY_STDOUT_MARKERS = ['Now listening on:', 'Application started'];

/**
 * Spawns and manages the FwLiteWeb binary for an e2e test. Each test owns its
 * own launcher (and process) so user-data directories don't bleed between tests.
 */
export class FwLiteLauncher {
  private process: ChildProcess | null = null;
  private baseUrl = '';
  private isHealthy = false;

  async launch(config: LaunchConfig): Promise<void> {
    if (this.process) throw new Error('FW Lite is already running. Call shutdown() first.');

    await assertExecutable(config.binaryPath);

    const port = config.port ?? await findAvailablePort(5000);
    this.baseUrl = `http://localhost:${port}`;

    await this.spawnProcess(config);
    await this.waitForHealthy(config.timeout ?? DEFAULT_LAUNCH_TIMEOUT_MS);
  }

  async shutdown(): Promise<void> {
    if (!this.process) return;
    this.isHealthy = false;
    const proc = this.process;

    if (platform() === 'win32') {
      // Windows can't SIGTERM a child; FwLiteWeb listens for "shutdown" on stdin.
      proc.stdin?.write('shutdown\n');
      proc.stdin?.end();
    } else {
      proc.kill('SIGTERM');
    }

    const exited = new Promise<void>(resolve => proc.once('exit', () => resolve()));
    const timedOut = delay(SHUTDOWN_TIMEOUT_MS).then(() => {
      if (!proc.killed) proc.kill('SIGKILL');
    });
    await Promise.race([exited, timedOut]);

    this.process = null;
    this.baseUrl = '';
  }

  isRunning(): boolean {
    return this.process !== null && !this.process.killed && this.isHealthy;
  }

  getBaseUrl(): string {
    if (!this.isRunning()) throw new Error('FW Lite is not running');
    return this.baseUrl;
  }

  private spawnProcess(config: LaunchConfig): Promise<void> {
    const args = [
      '--urls', this.baseUrl,
      '--Auth:LexboxServers:0:Authority', config.serverUrl,
      '--Auth:LexboxServers:0:DisplayName', 'e2e test server',
      '--FwLiteWeb:OpenBrowser', 'false',
      // Required so OAuth accepts the kind cluster's self-signed cert.
      '--environment', 'Development',
      // Serve the published viewer assets, not vite-served ones.
      '--FwLite:UseDevAssets', 'false',
    ];
    if (config.logFile) args.push('--FwLiteWeb:LogFileName', config.logFile);

    return new Promise((resolve, reject) => {
      const proc = spawn(config.binaryPath, args, {stdio: ['pipe', 'pipe', 'pipe']});
      this.process = proc;

      proc.on('error', error => reject(new Error(`Failed to start FW Lite: ${error.message}`)));
      proc.on('exit', (code, signal) => {
        // Clear state so isRunning() reflects a crash that happens after a successful launch;
        // the reject is a no-op once the startup promise has already settled.
        this.isHealthy = false;
        if (this.process === proc) this.process = null;
        reject(new Error(`FW Lite exited before becoming ready (code=${code}, signal=${signal})`));
      });
      proc.stdout?.on('data', (data: Buffer) => {
        const output = data.toString();
        if (READY_STDOUT_MARKERS.some(marker => output.includes(marker))) resolve();
      });
      proc.stderr?.on('data', (data: Buffer) => console.error('FW Lite stderr:', data.toString()));
    });
  }

  private async waitForHealthy(timeout: number): Promise<void> {
    const deadline = Date.now() + timeout;
    while (Date.now() < deadline) {
      if (await this.healthCheck()) {
        this.isHealthy = true;
        return;
      }
      await delay(HEALTH_CHECK_INTERVAL_MS);
    }
    throw new Error(`FW Lite failed to become healthy within ${timeout}ms`);
  }

  private async healthCheck(): Promise<boolean> {
    try {
      const response = await fetch(`${this.baseUrl}/health`, {
        signal: AbortSignal.timeout(HEALTH_CHECK_REQUEST_TIMEOUT_MS),
      });
      return response.ok;
    } catch {
      return false;
    }
  }
}

async function assertExecutable(binaryPath: string): Promise<void> {
  try {
    await access(binaryPath, constants.F_OK | constants.X_OK);
  } catch (error) {
    const message = error instanceof Error ? error.message : String(error);
    throw new Error(`FW Lite binary not found or not executable: ${binaryPath}. Error: ${message}`);
  }
}

function findAvailablePort(startPort: number): Promise<number> {
  return new Promise((resolve, reject) => {
    const server = createServer();
    server.listen(startPort, () => {
      const {port} = server.address() as AddressInfo;
      server.close(() => resolve(port));
    });
    server.on('error', (err: NodeJS.ErrnoException) => {
      if (err.code === 'EADDRINUSE') findAvailablePort(startPort + 1).then(resolve, reject);
      else reject(err);
    });
  });
}

function delay(ms: number): Promise<void> {
  return new Promise(resolve => setTimeout(resolve, ms));
}

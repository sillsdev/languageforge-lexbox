/**
 * FW Lite Application Launcher
 *
 * Manages the FW Lite application lifecycle during tests.
 * Handles launching, health checking, and shutting down the FW Lite application.
 */

import { spawn, type ChildProcess } from 'node:child_process';
import { access, constants } from 'node:fs/promises';
import { platform } from 'node:os';
import type { FwLiteManager, LaunchConfig } from '../types';

export class FwLiteLauncher implements FwLiteManager {
  private process: ChildProcess | null = null;
  private baseUrl = '';
  private port = 0;
  private isHealthy = false;

  /**
   * Launch the FW Lite application
   */
  async launch(config: LaunchConfig): Promise<void> {
    if (this.process) {
      throw new Error('FW Lite is already running. Call shutdown() first.');
    }

    // Validate binary exists and is executable
    await this.validateBinary(config.binaryPath);

    // Find available port
    this.port = config.port || await this.findAvailablePort(5000);
    this.baseUrl = `http://localhost:${this.port}`;

    // Launch the application
    await this.launchProcess(config);

    // Wait for application to be ready
    await this.waitForHealthy(config.timeout || 30000);
  }

  /**
   * Shutdown the FW Lite application
   */
  async shutdown(): Promise<void> {
    if (!this.process) {
      return;
    }

    this.isHealthy = false;

    // Try graceful shutdown first
    if (platform() === 'win32') {
      this.process.kill('SIGTERM');
    } else {
      this.process.kill('SIGTERM');
    }

    // Wait for graceful shutdown
    const shutdownPromise = new Promise<void>((resolve) => {
      if (!this.process) {
        resolve();
        return;
      }

      this.process.on('exit', () => {
        resolve();
      });
    });

    // Force kill after timeout
    const timeoutPromise = new Promise<void>((resolve) => {
      setTimeout(() => {
        if (this.process && !this.process.killed) {
          this.process.kill('SIGKILL');
        }
        resolve();
      }, 10000); // 10 second timeout
    });

    await Promise.race([shutdownPromise, timeoutPromise]);

    this.process = null;
    this.baseUrl = '';
    this.port = 0;
  }

  /**
   * Check if the application is running
   */
  isRunning(): boolean {
    return this.process !== null && !this.process.killed && this.isHealthy;
  }

  /**
   * Get the base URL of the running application
   */
  getBaseUrl(): string {
    if (!this.isRunning()) {
      throw new Error('FW Lite is not running');
    }
    return this.baseUrl;
  }

  /**
   * Validate that the binary exists and is executable
   */
  private async validateBinary(binaryPath: string): Promise<void> {
    try {
      await access(binaryPath, constants.F_OK | constants.X_OK);
    } catch (error) {
      throw new Error(`FW Lite binary not found or not executable: ${binaryPath}. Error: ${error}`);
    }
  }

  /**
   * Find an available port starting from the given port
   */
  private async findAvailablePort(startPort: number): Promise<number> {
    const net = await import('node:net');

    return new Promise((resolve, reject) => {
      const server = net.createServer();

      server.listen(startPort, () => {
        const port = (server.address() as any)?.port;
        server.close(() => {
          resolve(port);
        });
      });

      server.on('error', (err: any) => {
        if (err.code === 'EADDRINUSE') {
          // Port is in use, try next one
          this.findAvailablePort(startPort + 1).then(resolve).catch(reject);
        } else {
          reject(err);
        }
      });
    });
  }

  /**
   * Launch the FW Lite process
   */
  private async launchProcess(config: LaunchConfig): Promise<void> {
    return new Promise((resolve, reject) => {
      const args = [
        '--urls', this.baseUrl,
        '--server', config.serverUrl,
        '--FwLiteWeb:OpenBrowser', 'false'
      ];

      this.process = spawn(config.binaryPath, args, {
        stdio: ['ignore', 'pipe', 'pipe'],
        detached: false,
      });

      // Handle process events
      this.process.on('error', (error) => {
        reject(new Error(`Failed to start FW Lite: ${error.message}`));
      });

      this.process.on('exit', (code, signal) => {
        if (code !== 0 && code !== null) {
          reject(new Error(`FW Lite exited with code ${code}`));
        } else if (signal) {
          reject(new Error(`FW Lite was killed with signal ${signal}`));
        }
      });

      // Capture stdout/stderr for debugging
      if (this.process.stdout) {
        this.process.stdout.on('data', (data) => {
          const output = data.toString();
          // Look for startup indicators
          if (output.includes('Now listening on:') || output.includes('Application started')) {
            resolve();
          }
        });
      }

      if (this.process.stderr) {
        this.process.stderr.on('data', (data) => {
          console.error('FW Lite stderr:', data.toString());
        });
      }

      // Fallback timeout for process startup
      setTimeout(() => {
        resolve();
      }, 5000);
    });
  }

  /**
   * Wait for the application to be healthy and responsive
   */
  private async waitForHealthy(timeout: number): Promise<void> {
    const startTime = Date.now();
    const checkInterval = 1000; // Check every second

    while (Date.now() - startTime < timeout) {
      try {
        const isHealthy = await this.performHealthCheck();
        if (isHealthy) {
          this.isHealthy = true;
          return;
        }
      } catch (error) {
        // Health check failed, continue waiting
      }

      await new Promise(resolve => setTimeout(resolve, checkInterval));
    }

    throw new Error(`FW Lite failed to become healthy within ${timeout}ms`);
  }

  /**
   * Perform a health check on the running application
   */
  private async performHealthCheck(): Promise<boolean> {
    try {
      // Try to fetch a basic endpoint to verify the app is responding
      const response = await fetch(`${this.baseUrl}/health`, {
        method: 'GET',
        signal: AbortSignal.timeout(5000), // 5 second timeout
      });

      return response.ok;
    } catch (error) {
      // If /health doesn't exist, try the root endpoint
      try {
        const response = await fetch(this.baseUrl, {
          method: 'GET',
          signal: AbortSignal.timeout(5000),
        });

        // Accept any response that isn't a connection error
        return response.status < 500;
      } catch (rootError) {
        return false;
      }
    }
  }
}

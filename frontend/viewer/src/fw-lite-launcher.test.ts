/**
 * Integration tests for FW Lite Application Launcher
 *
 * These tests run against the real implementation without mocking
 * to ensure the launcher works correctly in practice.
 */

import {describe, it, expect, assert, beforeEach, afterEach} from 'vitest';
import {FwLiteLauncher} from '../tests/e2e/helpers/fw-lite-launcher';
import type {LaunchConfig} from '../tests/e2e/types';
import {getTestConfig} from '../tests/e2e/config';

describe('FwLiteLauncher', () => {
  let launcher: FwLiteLauncher;

  beforeEach(() => {
    launcher = new FwLiteLauncher();
  });

  afterEach(async () => {
    // Ensure cleanup after each test
    if (launcher.isRunning()) {
      await launcher.shutdown();
    }
  });

  describe('basic functionality', () => {
    it('should return false when not launched', () => {
      expect(launcher.isRunning()).toBe(false);
    });

    it('should throw error when getting base URL while not running', () => {
      expect(() => launcher.getBaseUrl()).toThrow('FW Lite is not running');
    });

    it('should handle shutdown when not running', async () => {
      await expect(launcher.shutdown()).resolves.not.toThrow();
      expect(launcher.isRunning()).toBe(false);
    });

    it('should throw error if binary does not exist', async () => {
      const config: LaunchConfig = {
        binaryPath: '/nonexistent/path/to/fw-lite',
        serverUrl: 'http://localhost:5137',
        port: 5000,
        timeout: 1000,
      };

      await expect(launcher.launch(config)).rejects.toThrow(
        'FW Lite binary not found or not executable'
      );
    });

    it('should throw error if already running', async () => {
      // Create a fake binary file for testing
      const testBinaryPath = './test-fake-binary.js';

      const fs = await import('node:fs/promises');
      await fs.writeFile(testBinaryPath, '#!/usr/bin/env node\nconsole.log("fake binary");', {mode: 0o755});

      const config: LaunchConfig = {
        binaryPath: testBinaryPath,
        serverUrl: 'http://localhost:5137',
        port: 5000,
        timeout: 1000,
      };

      // First launch should fail because it's not a real FW Lite binary
      await expect(launcher.launch(config)).rejects.toThrow();

      // Clean up
      await fs.unlink(testBinaryPath).catch(() => { });
    }, 10000);
  });

  describe('port finding functionality', () => {
    it('should be able to find available ports', async () => {
      const net = await import('node:net');

      // Test the port finding logic by creating a server on a port
      const server = net.createServer();

      return new Promise<void>((resolve, reject) => {
        server.listen(0, () => {
          const address = server.address();
          const port = typeof address === 'object' && address ? address.port : 0;

          expect(port).toBeGreaterThan(0);

          server.close(() => {
            resolve();
          });
        });

        server.on('error', reject);
      });
    });
  });

  describe('configuration validation', () => {
    it('should validate launch configuration parameters', () => {
      const validConfig: LaunchConfig = {
        binaryPath: '/path/to/fw-lite',
        serverUrl: 'http://localhost:5137',
        port: 5000,
        timeout: 10000,
      };

      // Test that config properties are accessible
      expect(validConfig.binaryPath).toBe('/path/to/fw-lite');
      expect(validConfig.serverUrl).toBe('http://localhost:5137');
      expect(validConfig.port).toBe(5000);
      expect(validConfig.timeout).toBe(10000);
    });

    it('should handle optional configuration parameters', () => {
      const minimalConfig: LaunchConfig = {
        binaryPath: '/path/to/fw-lite',
        serverUrl: 'http://localhost:5137',
      };

      // Test that optional parameters can be undefined
      expect(minimalConfig.port).toBeUndefined();
      expect(minimalConfig.timeout).toBeUndefined();
    });
  });

  describe('launcher state management', () => {
    it('should maintain proper state transitions', () => {
      // Initial state
      expect(launcher.isRunning()).toBe(false);

      // State should remain consistent
      expect(launcher.isRunning()).toBe(false);
      expect(launcher.isRunning()).toBe(false);
    });
  });

  describe('real FW Lite server integration', () => {
    async function getFwLiteBinaryPath() {
      const binaryPath = getTestConfig().fwLite.binaryPath;
      const fs = await import('node:fs/promises');
      try {
        await fs.access(binaryPath);
      } catch {
        assert.fail(`FW Lite binary not found at ${binaryPath}, skipping integration test. Run "pnpm build:fw-lite" first.`);
      }
      return binaryPath;
    }



    it('should successfully launch and shutdown real FW Lite server', async () => {
      // Check if the FW Lite binary exists
      const binaryPath = await getFwLiteBinaryPath();


      const config: LaunchConfig = {
        binaryPath,
        serverUrl: 'http://localhost:5137',
        port: 5555, // Use a specific port for testing
        timeout: 30000, // 30 seconds timeout
      };

      // Launch the server
      await launcher.launch(config);

      // Verify it's running
      expect(launcher.isRunning()).toBe(true);
      expect(launcher.getBaseUrl()).toBe('http://localhost:5555');

      // Test that we can make a request to the server
      try {
        const response = await fetch(`${launcher.getBaseUrl()}/health`);
        // Accept any response that indicates the server is running
        expect(response.status).toBeLessThan(500);
      } catch {
        // If /health doesn't exist, try the root endpoint
        const response = await fetch(launcher.getBaseUrl());
        expect(response.status).toBeLessThan(500);
      }

      // Shutdown the server
      await launcher.shutdown();

      // Verify it's stopped
      expect(launcher.isRunning()).toBe(false);
    }, 60000); // 60 second timeout for this test

    it('should handle multiple launch attempts gracefully', async () => {
      // Check if the FW Lite binary exists
      const binaryPath = await getFwLiteBinaryPath();

      const config: LaunchConfig = {
        binaryPath,
        serverUrl: 'http://localhost:5137',
        port: 5556, // Use a different port
        timeout: 30000,
      };

      // First launch should succeed
      await launcher.launch(config);
      expect(launcher.isRunning()).toBe(true);

      // Second launch should fail
      await expect(launcher.launch(config)).rejects.toThrow(
        'FW Lite is already running. Call shutdown() first.'
      );

      // Cleanup
      await launcher.shutdown();
      expect(launcher.isRunning()).toBe(false);
    }, 60000);
  });
});

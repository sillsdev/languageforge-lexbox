/**
 * E2E Test Configuration and Constants
 */

import type { E2ETestConfig, TestProject } from './types';

/**
 * Default test configuration
 */
export const DEFAULT_E2E_CONFIG: E2ETestConfig = {
  lexboxServer: {
    hostname: process.env.TEST_SERVER_HOSTNAME || 'localhost',
    // The CI kind cluster serves HTTP on `ingress-controller-port` (6579). Override
    // when pointing at a real https deployment.
    protocol: (process.env.TEST_SERVER_PROTOCOL as 'http' | 'https') || 'http',
    port: process.env.TEST_SERVER_PORT ? parseInt(process.env.TEST_SERVER_PORT) : 6579,
  },
  fwLite: {
    binaryPath: process.env.FW_LITE_BINARY_PATH || './dist/fw-lite-server/FwLiteWeb.exe',
    launchTimeout: 30000, // 30 seconds
    shutdownTimeout: 10000, // 10 seconds
  },
  testData: {
    projectCode: process.env.TEST_PROJECT_CODE || 'sena-3',
    testUser: process.env.TEST_USER || 'manager',
    testPassword: process.env.TEST_DEFAULT_PASSWORD || 'pass',
  },
  timeouts: {
    projectDownload: 60000, // 60 seconds
    entryCreation: 30000, // 30 seconds
    dataSync: 45000, // 45 seconds
  },
};

/**
 * Test project configurations
 */
export const TEST_PROJECTS: Record<string, TestProject> = {
  'sena-3': {
    code: 'sena-3',
    name: 'Sena 3',
    expectedEntries: 0, // Will be updated based on actual project state
    testUser: 'admin',
  },
};

/**
 * Get test configuration with environment variable overrides
 */
export function getTestConfig(): E2ETestConfig {
  return {
    ...DEFAULT_E2E_CONFIG,
    lexboxServer: {
      ...DEFAULT_E2E_CONFIG.lexboxServer,
      hostname: process.env.TEST_SERVER_HOSTNAME || DEFAULT_E2E_CONFIG.lexboxServer.hostname,
    },
    fwLite: {
      ...DEFAULT_E2E_CONFIG.fwLite,
      binaryPath: process.env.FW_LITE_BINARY_PATH || DEFAULT_E2E_CONFIG.fwLite.binaryPath,
    },
    testData: {
      ...DEFAULT_E2E_CONFIG.testData,
      projectCode: process.env.TEST_PROJECT_CODE || DEFAULT_E2E_CONFIG.testData.projectCode,
      testUser: process.env.TEST_USER || DEFAULT_E2E_CONFIG.testData.testUser,
      testPassword: process.env.TEST_DEFAULT_PASSWORD || DEFAULT_E2E_CONFIG.testData.testPassword,
    },
  };
}

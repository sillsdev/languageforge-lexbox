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
    protocol: 'https',
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
 * Test data constants
 */
export const TEST_CONSTANTS = {
  // Unique identifier prefix for test entries to avoid conflicts
  TEST_ENTRY_PREFIX: 'e2e-test',

  // Default test entry data
  DEFAULT_TEST_ENTRY: {
    lexeme: 'test-word',
    definition: 'A word created during E2E testing',
    partOfSpeech: 'noun',
  },

  // Retry configuration for flaky operations
  RETRY_CONFIG: {
    projectDownload: { attempts: 3, delay: 5000 },
    entryCreation: { attempts: 2, delay: 2000 },
    dataSync: { attempts: 3, delay: 3000 },
  },

  // UI selectors (to be updated based on actual FW Lite UI)
  SELECTORS: {
    projectList: '[data-testid="project-list"]',
    downloadButton: '[data-testid="download-project"]',
    newEntryButton: '[data-testid="new-entry"]',
    entryForm: '[data-testid="entry-form"]',
    saveButton: '[data-testid="save-entry"]',
    searchInput: '[data-testid="search-entries"]',
    deleteProjectButton: '[data-testid="delete-project"]',
  },
} as const;

/**
 * Generate unique test identifier
 */
export function generateTestId(): string {
  const timestamp = Date.now();
  const random = Math.random().toString(36).substring(2, 8);
  return `${TEST_CONSTANTS.TEST_ENTRY_PREFIX}-${timestamp}-${random}`;
}

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

/**
 * TypeScript type definitions for E2E tests
 */

export interface E2ETestConfig {
  lexboxServer: {
    hostname: string;
    protocol: 'http' | 'https';
    port?: number;
  };
  fwLite: {
    binaryPath: string;
    launchTimeout: number;
    shutdownTimeout: number;
  };
  testData: {
    projectCode: string;
    testUser: string;
    testPassword: string;
  };
  timeouts: {
    projectDownload: number;
    entryCreation: number;
    dataSync: number;
  };
}

export interface TestProject {
  code: string;
  name: string;
  expectedEntries: number;
  testUser: string;
}

export interface TestEntry {
  lexeme: string;
  definition: string;
  partOfSpeech: string;
  uniqueIdentifier: string;
}

export interface LaunchConfig {
  binaryPath: string;
  serverUrl: string;
  port?: number;
  timeout?: number;
}

export interface TestResult {
  testName: string;
  status: 'passed' | 'failed' | 'skipped';
  duration: number;
  error?: string;
  screenshots: string[];
  logs: string[];
}

export interface FwLiteManager {
  launch(config: LaunchConfig): Promise<void>;
  shutdown(): Promise<void>;
  isRunning(): boolean;
  getBaseUrl(): string;
}

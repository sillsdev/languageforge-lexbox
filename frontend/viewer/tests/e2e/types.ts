export interface E2ETestConfig {
  lexboxServer: {
    hostname: string;
    protocol: 'http' | 'https';
    port: number;
  };
  fwLite: {
    binaryPath: string;
    launchTimeout: number;
  };
  testData: {
    projectCode: string;
    testUser: string;
    testPassword: string;
  };
}

export interface LaunchConfig {
  binaryPath: string;
  serverUrl: string;
  port?: number;
  timeout?: number;
  logFile?: string;
}

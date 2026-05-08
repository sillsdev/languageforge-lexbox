import type {E2ETestConfig} from './types';

export const testConfig: E2ETestConfig = {
  lexboxServer: {
    hostname: process.env.TEST_SERVER_HOSTNAME || 'localhost',
    // Kind cluster in CI serves HTTP on 6579; override to https/6580 for the
    // ingress port-forward (MSAL refuses non-https authorities).
    protocol: (process.env.TEST_SERVER_PROTOCOL as 'http' | 'https') || 'http',
    port: process.env.TEST_SERVER_PORT ? parseInt(process.env.TEST_SERVER_PORT) : 6579,
  },
  fwLite: {
    binaryPath: process.env.FW_LITE_BINARY_PATH || './dist/fw-lite-server/FwLiteWeb.exe',
    launchTimeout: 30_000,
  },
  testData: {
    projectCode: process.env.TEST_PROJECT_CODE || 'sena-3',
    testUser: process.env.TEST_USER || 'manager',
    testPassword: process.env.TEST_DEFAULT_PASSWORD || 'pass',
  },
};

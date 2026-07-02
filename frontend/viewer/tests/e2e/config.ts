export interface Server {
  hostname: string;
  protocol: 'http' | 'https';
  port: number;
}

// Kind cluster in CI serves HTTP on 6579; CI overrides to https/6580
// (the ingress port-forward). MSAL refuses non-https authorities.
export const lexboxServer: Server = {
  hostname: process.env.TEST_SERVER_HOSTNAME || 'localhost',
  protocol: (process.env.TEST_SERVER_PROTOCOL as 'http' | 'https') || 'http',
  port: process.env.TEST_SERVER_PORT ? parseInt(process.env.TEST_SERVER_PORT) : 6579,
};

const defaultBinaryName = process.platform === 'win32' ? 'FwLiteWeb.exe' : 'FwLiteWeb';
export const fwLiteBinaryPath = process.env.FW_LITE_BINARY_PATH || `./dist/fw-lite-server/${defaultBinaryName}`;
export const projectCode = process.env.TEST_PROJECT_CODE || 'sena-3';
export const testUser = process.env.TEST_USER || 'manager';
export const testPassword = process.env.TEST_DEFAULT_PASSWORD || 'pass';

export function serverUrl(s: Server): string {
  return `${s.protocol}://${s.hostname}:${s.port}`;
}

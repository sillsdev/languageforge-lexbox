export const serverHostname = process.env.TEST_SERVER_HOSTNAME ?? 'localhost';
export const isDev = process.env.NODE_ENV === 'development' || serverHostname.startsWith('localhost');
export const httpScheme = isDev ? 'http://' : 'https://';
export const serverBaseUrl = `${httpScheme}${serverHostname}`;
export const standardHgHostname = process.env.TEST_STANDARD_HG_HOSTNAME ?? 'hg.localhost';
export const resumableHgHostname = process.env.TEST_RESUMABLE_HG_HOSTNAME ?? 'resumable.localhost';
export const resumableBaseUrl = `${httpScheme}${resumableHgHostname}`;
export const projectCode = process.env.TEST_PROJECT_CODE ?? 'sena-3';
export const defaultPassword = process.env.TEST_DEFAULT_PASSWORD ?? 'pass';
export const authCookieName = '.LexBoxAuth';
export const invalidJwt = 'eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTY5OTM0ODY2NywiaWF0IjoxNjk5MzQ4NjY3fQ.f8N63gcD_iv-E_x0ERhJwARaBKnZnORaZGe0N2J0VGM';

export const TEST_TIMEOUT = 30_000;
export const TEST_TIMEOUT_2X = TEST_TIMEOUT * 2;

export enum HgProtocol {
  Hgweb,
  Resumable,
}

export function getTestHostName(protocol: HgProtocol) : string {
  switch (protocol) {
    case HgProtocol.Hgweb: return standardHgHostname;
    case HgProtocol.Resumable: return resumableHgHostname;
  }
}

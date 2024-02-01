export const serverHostname = process.env.TEST_SERVER_HOSTNAME ?? 'localhost';
export const isDev = process.env.NODE_ENV === 'development' || serverHostname.startsWith('localhost');
export const httpScheme = isDev ? 'http://' : 'https://';
export const serverBaseUrl = `${httpScheme}${serverHostname}`;
export const standardHgHostname = process.env.TEST_STANDARD_HG_HOSTNAME ?? 'hg.localhost';
export const resumableHgHostname = process.env.TEST_RESUMABLE_HG_HOSTNAME ?? 'resumable.localhost';
export const resumableBaseUrl = `${httpScheme}${resumableHgHostname}`;
export const projectCode = process.env.TEST_PROJECT_CODE ?? 'sena-3';
export const defaultPassword = process.env.TEST_DEFAULT_PASSWORD ?? 'pass';

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

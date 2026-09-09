export function resumableSendReceiveOrigin(pageHost: string, isDev: boolean): { protocol: string; hostname: string } {
  if (isDev) {
    return { protocol: 'http', hostname: `resumable.${pageHost.split(':')[0]}` };
  }
  if (pageHost.includes('develop') || pageHost.includes('.dev')) {
    return { protocol: 'https', hostname: 'resumable.lexbox.dev.languagetechnology.org' };
  }
  if (pageHost.includes('staging')) {
    return { protocol: 'https', hostname: 'resumable-staging.languagedepot.org' };
  }
  return { protocol: 'https', hostname: 'resumable.languageforge.org' };
}

export function buildSendReceiveUrl(
  login: string,
  password: string,
  projectCode: string,
  pageHost: string,
  isDev: boolean,
): string {
  const { protocol, hostname } = resumableSendReceiveOrigin(pageHost, isDev);
  const originAndPath = `${protocol}://${hostname}/${projectCode}`;
  if (!password) return originAndPath;
  return `${protocol}://${encodeURIComponent(login)}:${encodeURIComponent(password)}@${hostname}/${projectCode}`;
}

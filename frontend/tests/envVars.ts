export const serverHostname = process.env.TEST_SERVER_HOSTNAME ?? 'localhost';
export const isDev = process.env.NODE_ENV === 'development' || serverHostname.startsWith('localhost');
export const httpScheme = isDev ? 'http://' : 'https://';
export const serverBaseUrl = `${httpScheme}${serverHostname}`;
export const defaultPassword = process.env.TEST_DEFAULT_PASSWORD ?? 'pass';
export const testOrgId = process.env.TEST_ORG_ID ?? '292c80e6-a815-4cd1-9ea2-34bd01274de6';
export const elawaProjectId = process.env.ELAWA_PROJECT_ID ?? '9e972940-8a8e-4b29-a609-bdc2f93b3507';

export const authCookieName = '.LexBoxAuth';
export const invalidJwt = 'eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTY5OTM0ODY2NywiaWF0IjoxNjk5MzQ4NjY3fQ.f8N63gcD_iv-E_x0ERhJwARaBKnZnORaZGe0N2J0VGM';

export const TEST_TIMEOUT = 40_000;
export const TEST_TIMEOUT_2X = TEST_TIMEOUT * 2;
export const ACTION_TIMEOUT = 5_000;
export const EXPECT_TIMEOUT = 5_000;

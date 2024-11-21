export const serverHostname = process.env.TEST_SERVER_HOSTNAME ?? 'localhost';
export const isDev = process.env.NODE_ENV === 'development' || serverHostname.startsWith('localhost');
export const httpScheme = isDev ? 'http://' : 'https://';
export const serverBaseUrl = `${httpScheme}${serverHostname}`;
export const defaultPassword = process.env.TEST_DEFAULT_PASSWORD ?? 'pass';
export const authCookieName = '.LexBoxAuth';
export const invalidJwt = 'eyJhbGciOiJIUzI1NiJ9.eyJSb2xlIjoiQWRtaW4iLCJJc3N1ZXIiOiJJc3N1ZXIiLCJVc2VybmFtZSI6IkphdmFJblVzZSIsImV4cCI6MTY5OTM0ODY2NywiaWF0IjoxNjk5MzQ4NjY3fQ.f8N63gcD_iv-E_x0ERhJwARaBKnZnORaZGe0N2J0VGM';

export const TEST_TIMEOUT = 40_000;
export const TEST_TIMEOUT_2X = TEST_TIMEOUT * 2;
export const ACTION_TIMEOUT = 5_000;
export const EXPECT_TIMEOUT = 5_000;

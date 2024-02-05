import { expect, type APIRequestContext } from '@playwright/test';
import { serverBaseUrl } from './envVars';

export async function loginAs(api: APIRequestContext, user: string, password: string): Promise<void> {
  const loginData = {
    emailOrUsername: user,
    password: password,
    preHashedPassword: false,
  }
  const response = await api.post(`${serverBaseUrl}/api/login`, {data: loginData});
  expect(response.ok()).toBeTruthy();
  await api.storageState({path: `${user}-storageState.json`});
}

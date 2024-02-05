import { expect, type APIRequestContext, type Page } from '@playwright/test';
import { serverBaseUrl } from './envVars';
import { RegisterPage } from './pages/registerPage';
import { UserDashboardPage } from './pages/userDashboardPage';
import type { UUID } from 'crypto';
import { executeGql } from './gqlHelpers';

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

export async function getCurrentUserId(api: APIRequestContext): Promise<UUID> {
  const response = await api.get(`${serverBaseUrl}/api/user/currentUser`);
  const user = await response.json() as {sub: UUID};
  expect(user).not.toBeNull();
  return user.sub;
}

export async function registerUser(page: Page, name: string, email: string, password: string): Promise<UUID> {
  const registerPage = await new RegisterPage(page).goto();
  await registerPage.fillForm(name, email, password);
  await registerPage.submit();
  await new UserDashboardPage(page).waitFor();
  const userId = await getCurrentUserId(page.request);
  return userId;
}

export async function deleteUser(api: APIRequestContext, userId: string): Promise<unknown> {
  return executeGql(api, `
  mutation {
    deleteUserByAdminOrSelf(input: { userId: "${userId}" }) {
        user {
            id
        }
        errors {
            ... on Error {
                message
            }
        }
    }
  }
  `);
}

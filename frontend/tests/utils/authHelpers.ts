import {expect, type APIRequestContext, type Page} from '@playwright/test';
import {defaultPassword, testOrgId, serverBaseUrl} from '../envVars';
import {RegisterPage} from '../pages/registerPage';
import {UserDashboardPage} from '../pages/userDashboardPage';
import type {UUID} from 'crypto';
import {executeGql} from './gqlHelpers';
import {LoginPage} from '../pages/loginPage';
import type {OrgRole, ProjectRole} from '$lib/gql/types';
import type {TempUser} from '../fixtures';
import {EmailSubjects} from '../email/email-page';

export async function loginAs(api: APIRequestContext, emailOrUsername: string, password: string = defaultPassword): Promise<void> {
  const loginData = {
    emailOrUsername: emailOrUsername,
    password: password,
    preHashedPassword: false,
  }
  const response = await api.post(`${serverBaseUrl}/api/login`, {data: loginData});
  expect(response.ok()).toBeTruthy();
}

export async function logout(page: Page): Promise<LoginPage> {
  await page.goto('/logout');
  return await new LoginPage(page).waitFor();
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

// Verify email address; returns a Page promise that should be used to load a UserDashboardPage or AdminDashboardPage
// Does not call loginAs(page.request, tempUser.email, tempUser.password); calling code should be doing that
export async function verifyTempUserEmail(page: Page, tempUser: TempUser): Promise<Page> {
  const emailPage = await tempUser.mailbox.openEmail(page, EmailSubjects.VerifyEmail);
  const pagePromise = emailPage.page.context().waitForEvent('page');
  await emailPage.clickVerifyEmail();
  return pagePromise;
}

export async function addUserToOrg(api: APIRequestContext, userId: string, orgId: string, role: OrgRole): Promise<unknown> {
  return executeGql(api, `
    mutation {
        changeOrgMemberRole(input: { userId: "${userId}", orgId: "${orgId}", role: ${role} }) {
            organization {
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

export async function addUserToProject(api: APIRequestContext, userId: string, projectId: string, role: ProjectRole): Promise<unknown> {
  return executeGql(api, `
    mutation {
        addProjectMember(input: { userId: "${userId}", projectId: "${projectId}", role: ${role} }) {
            errors {
                ... on Error {
                    message
                }
            }
        }
    }
  `);
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

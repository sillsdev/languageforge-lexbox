import { test as base, expect, type BrowserContext, type BrowserContextOptions } from '@playwright/test';
import * as testEnv from './envVars';
import { type UUID, randomUUID } from 'crypto';
import { deleteUser, loginAs, registerUser } from './utils/authHelpers';
import { executeGql } from './utils/gqlHelpers';

export interface TempUser {
  id: UUID
  mailinatorId: UUID
  name: string
  email: string
  password: string
}

export interface TempProject {
  id: UUID
  code: string
  name: string
}

type Fixtures = {
  contextFactory: (options: BrowserContextOptions) => Promise<BrowserContext>,
  tempUser: TempUser,
  tempProject: TempProject
}

function addUnexpectedResponseListener(context: BrowserContext): void {
  context.addListener('response', response => {
    expect.soft(response.status(), `Unexpected response: ${response.status()}`).toBeLessThan(500);
    if (response.request().isNavigationRequest()) {
      expect.soft(response.status(), `Unexpected response: ${response.status()}`).toBeLessThan(400);
    }
  });
}

export const test = base.extend<Fixtures>({
  contextFactory: async ({ browser }, use) => {
    const contexts: BrowserContext[] = [];
    await use(async (options) => {
      const context = await browser.newContext(options);
      contexts.push(context);
      addUnexpectedResponseListener(context);
      return context;
    });
    for (const context of contexts) {
      await context.close();
    }
  },
  context: async ({ context }, use) => {
    addUnexpectedResponseListener(context);
    await use(context);
  },
  tempUser: async ({ browser, page }, use, testInfo) => {
    const mailinatorId = randomUUID();
    const email = `${mailinatorId}@mailinator.com`;
    const name = `Test: ${testInfo.title} - ${email}`;
    const password = email;
    const tempUserId = await registerUser(page, name, email, password);
    const tempUser: TempUser = {
      id: tempUserId,
      mailinatorId,
      name,
      email,
      password
    };
    await use(tempUser);
    const context = await browser.newContext();
    await loginAs(context.request, 'admin', testEnv.defaultPassword);
    await deleteUser(context.request, tempUser.id);
    await context.close();
  },
  tempProject: async ({ page }, use, testInfo) => {
    const titleForCode =
      testInfo.title
      .replaceAll(' ','-')
      .replaceAll(/[^a-z-]/g,'');
    const code = `test-${titleForCode}-${testInfo.testId}`;
    const name = `Temporary project for ${testInfo.title} unit test ${testInfo.testId}`;
    const loginData = {
      emailOrUsername: 'admin',
      password: testEnv.defaultPassword,
      preHashedPassword: false,
    }
    const response = await page.request.post(`${testEnv.serverBaseUrl}/api/login`, {data: loginData});
    expect(response.ok()).toBeTruthy();
    const gqlResponse = await executeGql(page.request, `
      mutation {
        createProject(input: {
          name: "${name}",
          type: FL_EX,
          code: "${code}",
          description: "temporary project created during the ${testInfo.title} unit test",
          retentionPolicy: DEV
        }) {
          createProjectResponse {
            id
            result
          }
          errors {
            __typename
            ... on DbError {
              code
              message
            }
          }
        }
      }
`);
    const id = (gqlResponse as {data: {createProject: {createProjectResponse: {id: UUID}}}}).data.createProject.createProjectResponse.id;
    await use({id, code, name});
    const deleteResponse = await page.request.delete(`${testEnv.serverBaseUrl}/api/project/project/${id}`);
    expect(deleteResponse.ok()).toBeTruthy();
  }
});

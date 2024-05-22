import { test as base, expect, type BrowserContext, type BrowserContextOptions } from '@playwright/test';
import * as testEnv from './envVars';
import { type UUID, randomUUID } from 'crypto';
import { deleteUser, loginAs, registerUser } from './utils/authHelpers';
import { executeGql } from './utils/gqlHelpers';
import { mkdtemp, rm } from 'fs/promises';
import { join } from 'path';
import { tmpdir } from 'os';

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

type CreateProjectResponse = {data: {createProject: {createProjectResponse: {id: UUID}}}}

type Fixtures = {
  contextFactory: (options: BrowserContextOptions) => Promise<BrowserContext>,
  uniqueTestId: string,
  tempUser: TempUser,
  tempProject: TempProject,
  tempDir: string,
}

function addUnexpectedResponseListener(context: BrowserContext): void {
  context.addListener('response', response => {
    const traceparent = response.request().headers()['Traceparent'];
    expect.soft(response.status(), `Unexpected response status: ${response.status()}. (${traceparent})`).toBeLessThan(500);
    if (response.request().isNavigationRequest()) {
      expect.soft(response.status(), `Unexpected response status: ${response.status()}. (${traceparent})`).toBeLessThan(400);
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
  // eslint-disable-next-line no-empty-pattern
  uniqueTestId: async ({ }, use, testInfo) => {
    // testInfo.testId is only guarunteed to be unique within a session (https://playwright.dev/docs/api/class-testcase#test-case-id)
    // i.e. it's not unique enough if a test fails to cleanup. We've had that problem.
    const shortId = randomUUID().split('-')[0];
    const testId = `${testInfo.testId}-${shortId}`;
    await use(testId);
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
  tempProject: async ({ page, uniqueTestId }, use, testInfo) => {
    const titleForCode =
      testInfo.title
      .replaceAll(' ','-')
        .replaceAll(/[^a-z-]/g, '');
    const code = `test-${titleForCode}-${uniqueTestId}`;
    const name = `Temporary project for ${testInfo.title} unit test ${uniqueTestId}`;
    const loginData = {
      emailOrUsername: 'admin',
      password: testEnv.defaultPassword,
      preHashedPassword: false,
    }
    const response = await page.request.post(`${testEnv.serverBaseUrl}/api/login`, {data: loginData});
    expect(response.ok()).toBeTruthy();
    const gqlResponse = await executeGql<CreateProjectResponse>(page.request, `
      mutation {
        createProject(input: {
          name: "${name}",
          type: FL_EX,
          code: "${code}",
          description: "temporary project created during the ${testInfo.title} unit test",
          retentionPolicy: DEV,
          isConfidential: false
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
    const id = gqlResponse.data.createProject.createProjectResponse.id;
    await use({id, code, name});
    const deleteResponse = await page.request.delete(`${testEnv.serverBaseUrl}/api/project/${id}`);
    expect(deleteResponse.ok()).toBeTruthy();
  },
  // eslint-disable-next-line no-empty-pattern
  tempDir: async ({ uniqueTestId }, use) => {
    const dirname = await mkdtemp(join(tmpdir(), `e2etmp-${uniqueTestId}-`));
    await use(dirname);
    try {
      await rm(dirname, { recursive: true, force: true });
    } catch (e) {
      // This fails frequently for me when running tests in Firefox and it's not critical
      const error = ((e && typeof e === 'object' && 'message' in e) ? e.message : e) as string;
      console.warn(`Failed to clean up temporary directory ${dirname}: ${error}.`);
    }
  }
});

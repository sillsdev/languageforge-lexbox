import {test as base, expect, type BrowserContext, type BrowserContextOptions} from '@playwright/test';
import * as testEnv from './envVars';
import {type UUID, randomUUID} from 'crypto';
import {deleteUser, loginAs, registerUser} from './utils/authHelpers';
import {executeGql, type GqlResult} from './utils/gqlHelpers';
import {mkdtemp, rm} from 'fs/promises';
import {join} from 'path';
import {tmpdir} from 'os';
import type {Mailbox} from './email/mailbox';
import {isDev} from './envVars';
import {E2EMailboxApi} from './email/e2e-mailbox-module-patched';
import {MaildevMailbox} from './email/maildev-mailbox';
import {E2EMailbox} from './email/e2e-mailbox';

export interface TempUser {
  id: UUID
  name: string
  email: string
  password: string
  mailbox: Mailbox
}

export interface TempProject {
  id: UUID
  code: string
  name: string
}

export type CreateProjectResponse = {data: {createProject: {createProjectResponse: {id: UUID}}}}

type Fixtures = {
  contextFactory: (options: BrowserContextOptions) => Promise<BrowserContext>,
  uniqueTestId: string,
  tempUser: Readonly<TempUser>,
  tempProject: TempProject,
  tempDir: string,
  mailboxFactory: () => Promise<Mailbox>,
}

async function getNewMailbox(context: BrowserContext): Promise<Mailbox> {
  if (isDev) {
    const email = `${randomUUID()}@maildev.com`;
    return new MaildevMailbox(email, context.request);
  } else {
    const mailbox = new E2EMailboxApi();
    const email = await mailbox.createEmailAddress();
    return new E2EMailbox(email, mailbox);
  }
}

function addUnexpectedResponseListener(context: BrowserContext): void {
  context.addListener('response', async (response) => {
    const traceparent = response.request().headers()['traceparent'];
    const status = response.status();
    const url = response.request().url();
    const unexpectedResponseMessage = `Unexpected response status: ${status}. (Request URL: ${url}. Traceparent: ${traceparent}.)`;
    expect.soft(response.status(), unexpectedResponseMessage).toBeLessThan(500);
    if (response.request().isNavigationRequest()) {
      expect.soft(response.status(), unexpectedResponseMessage).toBeLessThan(400);
    }
    if (url.endsWith('/api/graphql') && response.ok()) { // response.ok() filters out redirects, which don't have a response body
      const result = await response.json() as GqlResult;
      expect.soft(result.errors?.[0]?.message).not.toBe('Unexpected Execution Error');
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
  mailboxFactory: async ({context}, use) => {
    await use(() => getNewMailbox(context));
  },
  tempUser: async ({browser, page, mailboxFactory}, use, testInfo) => {
    const mailbox = await mailboxFactory();
    const email = mailbox.email;
    const name = `Test: ${testInfo.title} - ${email}`;
    const password = email;
    const tempUserId = await registerUser(page, name, email, password);
    const tempUser = Object.freeze({
      id: tempUserId,
      name,
      email,
      password,
      mailbox,
    });
    await use(tempUser);
    const context = await browser.newContext();
    await loginAs(context.request, 'admin');
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

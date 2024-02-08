import { test as base, expect, type BrowserContext, type BrowserContextOptions } from '@playwright/test';
import * as testEnv from './envVars';
import { type UUID, randomUUID } from 'crypto';
import { deleteUser, loginAs, registerUser } from './utils/authHelpers';

export interface TempUser {
  id: UUID
  mailinatorId: UUID
  name: string
  email: string
  password: string
}

type Fixtures = {
  tempUser: TempUser,
  contextFactory: (options: BrowserContextOptions) => Promise<BrowserContext>,
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
});

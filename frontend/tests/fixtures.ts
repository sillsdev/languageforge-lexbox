import { test as base, expect } from '@playwright/test';
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
  tempUser: TempUser
}

export const test = base.extend<Fixtures>({
  context: async ({ context }, use) => {
    context.addListener('response', response => {
      expect.soft(response.status(), `Unexpected response: ${response.status()}`).toBeLessThan(500);
      if (response.request().isNavigationRequest()) {
        expect.soft(response.status(), `Unexpected response: ${response.status()}`).toBeLessThan(400);
      }
    });
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

import { test as base } from '@playwright/test';
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
  tempUser: async ({ browser, page }, use) => {
    const mailinatorId = randomUUID();
    const email = `${mailinatorId}@mailinator.com`;
    const name = email;
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
  }
})

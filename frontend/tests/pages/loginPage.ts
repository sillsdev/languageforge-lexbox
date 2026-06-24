import type { Page } from '@playwright/test';
import { BasePage } from './basePage';
import { ForgotPasswordPage } from './forgotPasswordPage';
import { defaultPassword } from '../envVars';

export class LoginPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Log in'}), '/login');
  }

  async fillForm(emailOrUsername: string, password: string): Promise<void> {
      await this.page.getByLabel('Email').fill(emailOrUsername);
      await this.page.getByLabel('Password').fill(password);
  }

  submit(): Promise<void> {
      return this.page.getByRole('button', { name: 'Log in' }).click();
  }

  static async loginAsAdmin(page: Page): Promise<void> {
    const loginPage = await new LoginPage(page).goto();
    await loginPage.fillForm('admin', defaultPassword);
    await Promise.all([page.waitForURL((url) => !url.pathname.startsWith('/login')), loginPage.submit()]);
  }

  async clickForgotPassword(): Promise<ForgotPasswordPage> {
      await this.page.getByRole('link', { name: 'Forgot your password?' }).click();
      return await new ForgotPasswordPage(this.page).waitFor();
  }
}

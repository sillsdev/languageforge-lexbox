import type { Page } from '@playwright/test';
import { BasePage } from './basePage';
import { ForgotPasswordPage } from './forgotPasswordPage';

export class LoginPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Log in'}), '/login');
  }

  async fillForm(emailOrUsername: string, password: string): Promise<void>
  {
      await this.page.getByLabel('Email').fill(emailOrUsername);
      await this.page.getByLabel('Password').fill(password);
  }

  submit(): Promise<void>
  {
      return this.page.getByRole('button', { name: 'Log in' }).click();
  }

  async clickForgotPassword(): Promise<ForgotPasswordPage>
  {
      await this.page.getByRole('link', { name: 'Forgot your password?' }).click();
      return await new ForgotPasswordPage(this.page).waitFor();
  }
}

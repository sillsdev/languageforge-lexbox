import type { Page } from '@playwright/test';
import { BasePage } from './basePage';

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

  // TODO: Convert this after porting the ForgotPassword page
  // async clickForgotPassword()
  // {
  //     await Page.GetByRole(AriaRole.Link, new() { Name = "Forgot password" }).ClickAsync();
  //     return await new ForgotPasswordPage(Page).WaitFor();
  // }
}

import type { Page } from '@playwright/test';
import { BasePage } from './basePage';

export class ResetPasswordEmailSentPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Check Your Inbox'}), '/forgotPassword/emailSent');
  }
}

export class ForgotPasswordPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Forgot Password'}), '/forgotPassword');
  }

  async fillForm(email: string): Promise<void>
  {
      await this.page.getByLabel('Email').fill(email);
  }

  async submit(): Promise<ResetPasswordEmailSentPage>
  {
      await this.page.getByRole('button', { name: 'Send reset email' }).click();
      return await new ResetPasswordEmailSentPage(this.page).waitFor();
    }
}

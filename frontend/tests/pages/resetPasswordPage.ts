import type { Page } from '@playwright/test';
import { AuthenticatedBasePage } from './authenticatedBasePage';
import { UserDashboardPage } from './userDashboardPage';

export class ResetPasswordPage extends AuthenticatedBasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Reset Password', exact: true}), `/resetPassword`);
  }

  fillForm(newPassword: string): Promise<void> {
    return this.page.getByLabel('New Password').fill(newPassword);
  }

  async submit(): Promise<UserDashboardPage> {
    await this.page.getByRole('button', {name: 'Reset Password', exact: true}).click();
    return await new UserDashboardPage(this.page).waitFor();
  }
}

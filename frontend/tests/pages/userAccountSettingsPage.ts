import type { Page } from '@playwright/test';
import { AuthenticatedBasePage } from './authenticatedBasePage';
import { ResetPasswordPage } from './resetPasswordPage';

export class UserAccountSettingsPage extends AuthenticatedBasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'Account Settings'}), `/user`);
  }

  fillDisplayName(name: string): Promise<void> {
    return this.page.locator('form').getByLabel('Display name').fill(name);
  }

  fillEmail(email: string): Promise<void> {
    return this.page.locator('form').getByLabel('Email').fill(email);
  }

  async clickSave(): Promise<void> {
    await this.page.getByRole('button', {name: 'Update account'}).click();
    await this.page.waitForLoadState('domcontentloaded');
  }

  async clickResetPassword(): Promise<ResetPasswordPage> {
    await this.page.getByRole('link', {name: 'Reset your password'}).click();
    return await new ResetPasswordPage(this.page).waitFor();
  }
}

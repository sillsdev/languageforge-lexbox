import type { Locator, Page } from '@playwright/test';
import { BasePage } from './basePage';
import { EmailVerificationAlert } from '../components/emailVerificationAlert';

export class AuthenticatedBasePage extends BasePage {
  readonly emailVerificationAlert: EmailVerificationAlert;

  constructor(page: Page, locator: Locator | Locator[], url?: string) {
    if (Array.isArray(locator)) {
      locator = [page.locator('label .i-mdi-account-circle'), ...locator];
    } else {
      locator = [page.locator('label .i-mdi-account-circle'), locator];
    }
    super(page, locator, url);
    this.emailVerificationAlert = new EmailVerificationAlert(page);
  }

  clickHome(): Promise<void> {
    return this.page.locator('.breadcrumbs').getByRole('link', {name: 'Home'}).click();
  }
}

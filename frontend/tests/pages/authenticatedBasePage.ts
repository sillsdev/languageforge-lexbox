import type {Locator, Page} from '@playwright/test';

import {AuthenticatedDrawer} from '../components/authenticatedDrawer';
import {BasePage} from './basePage';
import {EmailVerificationAlert} from '../components/emailVerificationAlert';

export class AuthenticatedBasePage extends BasePage {
  readonly emailVerificationAlert: EmailVerificationAlert;

  private drawerToggle: Locator;

  constructor(page: Page, locator: Locator | Locator[], url?: string) {
    const drawerToggle = page.locator('label .i-mdi-account-circle');
    if (Array.isArray(locator)) {
      locator = [drawerToggle, ...locator];
    } else {
      locator = [drawerToggle, locator];
    }
    super(page, locator, url);
    this.drawerToggle = drawerToggle;
    this.emailVerificationAlert = new EmailVerificationAlert(page);
  }

  clickHome(): Promise<void> {
    return this.page.locator('.breadcrumbs').getByRole('link', {name: 'Home'}).click();
  }

  async openDrawer(): Promise<AuthenticatedDrawer> {
    await this.drawerToggle.click();
    return new AuthenticatedDrawer(this.page).waitFor();
  }
}

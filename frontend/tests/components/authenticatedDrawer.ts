import {type Locator, type Page} from '@playwright/test';
import {BaseComponent} from './baseComponent';
import {LoginPage} from '../pages/loginPage';

export class AuthenticatedDrawer extends BaseComponent {

  get logoutLink(): Locator {
    return this.componentLocator.getByRole('link', { name: 'Log out' });
  }

  constructor(page: Page) {
    super(page, page.locator('.drawer .drawer-side:has-text("Log out")'));
  }

  async logout(): Promise<LoginPage> {
    await this.logoutLink.click();
    return new LoginPage(this.page).waitFor();
  }
}

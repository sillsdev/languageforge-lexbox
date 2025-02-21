import {BasePage} from './basePage';
import type {Page} from '@playwright/test';

export class OauthApprovalPage extends BasePage {
  constructor(page: Page) {
    super(page, page.locator('.i-mdi-approval'), '/authorize');
  }

  async clickAuthorize(): Promise<void> {
    await this.page.getByRole('button', {name: 'Authorize'}).click();
  }
}

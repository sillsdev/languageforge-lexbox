import type {Page} from '@playwright/test';
import {BasePage} from './basePage';

export class OauthApprovalPage extends BasePage {
  constructor(page: Page) {
    super(page, page.locator('.i-mdi-check-decagram'), '/authorize');
  }

  async clickAuthorize(): Promise<void> {
    await this.page.getByRole('button', {name: 'Authorize'}).click();
  }
}

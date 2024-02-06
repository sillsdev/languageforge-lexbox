import type { Page } from '@playwright/test';
import { MailEmailPage, MailInboxPage } from './mailPages';

export class MailDevInboxPage extends MailInboxPage {
  constructor(page: Page, mailboxId: string) {
    super(page,
          page.locator('ul.email-list'),
          'http://localhost:1080/#/',
          mailboxId,
          page.locator('ul.email-list li a'));
  }

  override getEmailPage(): MailEmailPage {
    return new MailDevEmailPage(this.page);
  }

  override async goto(options?: {expectRedirect: boolean}): Promise<this> {
    await super.goto(options);
    await this.page.locator('input.search-input').fill(this.mailboxId);
    return this;
  }
}

export class MailDevEmailPage extends MailEmailPage {
  constructor(page: Page) {
    super(page, page.frameLocator('.preview-iframe:visible').locator('body'), undefined);
  }
}

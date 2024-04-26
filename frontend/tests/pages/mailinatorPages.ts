import type { Page } from '@playwright/test';
import { MailEmailPage, MailInboxPage } from './mailPages';

export class MailinatorInboxPage extends MailInboxPage {
  constructor(page: Page, mailboxId: string) {
    super(page,
          page.locator(`[id^='row_']`).first(),
          `https://www.mailinator.com/v4/public/inboxes.jsp?to=${mailboxId}`,
          mailboxId,
          page.locator(`[id^='row_']`));
  }

  override getEmailPage(): MailEmailPage {
    return new MailinatorEmailPage(this.page);
  }

  override async refreshEmails(): Promise<void> {
    return Promise.resolve(); // Mailinator auto-refreshes
  }

  override async goto(options?: {expectRedirect: boolean}): Promise<this> {
    this.url = `https://www.mailinator.com/v4/public/inboxes.jsp?to=${this.mailboxId}`;
    return super.goto(options);
  }
}

export class MailinatorEmailPage extends MailEmailPage {
  constructor(page: Page) {
    super(page, page.frameLocator('#html_msg_body').locator('body'), undefined);
  }

  override getFirstLanguageDepotUrl(): Promise<string | null> {
    // Mailinator sometimes swaps links out with its own that and redirect to the original,
    // but the originals are made available in the links tab, which is always in the DOM
    return this.page.locator('#email_pane').locator(`a[href*='jwt=']`).first().getAttribute('href');
  }
}

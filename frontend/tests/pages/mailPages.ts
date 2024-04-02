import type { Locator, Page } from '@playwright/test';
import { BasePage } from './basePage';
import { serverBaseUrl } from '../envVars';

export abstract class MailInboxPage extends BasePage {
  public mailboxId: string;
  readonly emailLocator: Locator;

  constructor(page: Page, testLocator: Locator, url: string, mailboxId: string, emailLocator: Locator) {
    super(page, testLocator, url);
    this.mailboxId = mailboxId;
    this.emailLocator = emailLocator;
  }

  abstract getEmailPage(): MailEmailPage;

  async gotoMailbox(mailboxId: string): Promise<MailInboxPage> {
    this.mailboxId = mailboxId;
    return await this.goto();
  }

  async openEmail(index = 0): Promise<MailEmailPage> {
    await this.emailLocator.nth(index).click();
    return await this.getEmailPage().waitFor();
  }
}

export abstract class MailEmailPage extends BasePage {
  readonly bodyLocator: Locator;
  public get resetPasswordButton(): Locator { return this.bodyLocator.getByRole('link', {name: 'Reset password'}); }

  constructor(page: Page, bodyLocator: Locator, url?: string) {
    super(page, bodyLocator, url);
    this.bodyLocator = bodyLocator;
  }

  clickVerifyEmail(): Promise<void> {
    return this.bodyLocator.getByRole('link', {name: 'Verify e-mail'}).click();
  }

  clickResetPassword(): Promise<void> {
    return this.resetPasswordButton.click();
  }

  getFirstLanguageDepotUrl(): Promise<string | null> {
    return this.bodyLocator.locator(`a[href*='${serverBaseUrl}']`).first().getAttribute('href');
  }

  clickFirstLanguageDepotUrl(): Promise<void> {
    return this.bodyLocator.locator(`a[href*='${serverBaseUrl}']`).first().click();
  }
}

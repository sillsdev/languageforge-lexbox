import { type Locator, type Page, expect } from '@playwright/test';
import { BasePage } from './basePage';
import { serverBaseUrl } from '../envVars';

export enum EmailSubjects {
  VerifyEmail = 'Verify your e-mail address',
  ForgotPassword = 'Forgot your password?',
  PasswordChanged = 'Your password was changed',
  ProjectInvitation = 'Project invitation:',
}

export abstract class MailInboxPage extends BasePage {
  public mailboxId: string;
  readonly emailLocator: Locator;

  constructor(page: Page, testLocator: Locator, url: string, mailboxId: string, emailLocator: Locator) {
    super(page, testLocator, url);
    this.mailboxId = mailboxId;
    this.emailLocator = emailLocator;
  }

  abstract getEmailPage(): MailEmailPage;
  abstract refreshEmails(): Promise<void>;

  async gotoMailbox(mailboxId: string): Promise<MailInboxPage> {
    this.mailboxId = mailboxId;
    return await this.goto();
  }

  async openEmail(subject: EmailSubjects, index = 0): Promise<MailEmailPage> {
    const email = this.emailLocator.locator(`:text("${subject}")`).nth(index);
    // Emails may not be immediately available, so if they aren't, refresh the email list until they show up
    await expect(async () => {
      if (!await email.isVisible()) {
        await this.refreshEmails();
      }
      await email.click();
    }, `Failed to find email: ${subject} (${index})`).toPass({timeout: 10_000}); // This auto-retries on a reasonable schedule
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

import {type Locator, type Page} from '@playwright/test';
import {BasePage} from '../pages/basePage';
import {serverBaseUrl} from '../envVars';

export const enum EmailSubjects {
  VerifyEmail = 'Verify your e-mail address',
  ForgotPassword = 'Forgot your password?',
  PasswordChanged = 'Your password was changed',
  ProjectInvitation = 'Project invitation:',
  ProjectJoinRequest = 'Project join request',
}

export class EmailPage extends BasePage {
  readonly bodyLocator: Locator;
  public get resetPasswordButton(): Locator { return this.bodyLocator.getByRole('link', {name: 'Reset password'}); }

  constructor(page: Page, url?: string) {
    super(page, page.locator('body', {has: page.locator('a[href*="https://lexbox.org"]').first()}), url);
    this.bodyLocator = this.locators[0];
  }

  clickVerifyEmail(): Promise<void> {
    return this.bodyLocator.getByRole('link', {name: 'Verify e-mail'}).click();
  }

  clickApproveRequest(): Promise<void> {
    return this.bodyLocator.getByRole('link', {name: 'Approve request'}).click();
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

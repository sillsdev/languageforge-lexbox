import { expect, type Page } from '@playwright/test';
import { BaseComponent } from './baseComponent';

const PLEASE_VERIFY_SELECTOR = `:text('We sent you an email, so that you can verify your email address')`;
const VERIFICATION_SENT_SELECTOR = `:text('check your inbox for the verification email')`;
const SUCCESSFULLY_VERIFIED_SELECTOR = `:text('successfully verified')`;
const REQUESTED_TO_CHANGE_SELECTOR = `:text('requested to change')`;
const SUCCESSFULLY_UPDATED_SELECTOR = `:text('successfully updated')`;
const CONTENT_SELECTOR = [
  PLEASE_VERIFY_SELECTOR,
  VERIFICATION_SENT_SELECTOR,
  SUCCESSFULLY_VERIFIED_SELECTOR,
  REQUESTED_TO_CHANGE_SELECTOR,
  SUCCESSFULLY_UPDATED_SELECTOR,
].join(', ');

export class EmailVerificationAlert extends BaseComponent {
  constructor(page: Page) {
    super(page, page.locator('.alert', {has: page.locator(CONTENT_SELECTOR)}));
  }

  assertPleaseVerify(): Promise<void> {
    return expect(this.componentLocator.locator(PLEASE_VERIFY_SELECTOR)).toBeVisible();
  }

  assertVerificationSent(): Promise<void> {
    return expect(this.componentLocator.locator(VERIFICATION_SENT_SELECTOR)).toBeVisible();
  }

  assertSuccessfullyVerified(): Promise<void> {
    return expect(this.componentLocator.locator(SUCCESSFULLY_VERIFIED_SELECTOR)).toBeVisible();
  }

  assertRequestedToChange(): Promise<void> {
    return expect(this.componentLocator.locator(REQUESTED_TO_CHANGE_SELECTOR)).toBeVisible();
  }

  assertSuccessfullyUpdated(): Promise<void> {
    return expect(this.componentLocator.locator(SUCCESSFULLY_UPDATED_SELECTOR)).toBeVisible();
  }

  public clickResenrEmail(): Promise<void> {
    return this.page.getByRole('button', {name: 'Resend verification email'}).click();
  }
}

import type { Page } from '@playwright/test';
import { isDev } from './envVars';
import type { MailInboxPage } from './pages/mailPages';
import { MailDevInboxPage } from './pages/mailDevPages';
import { MailinatorInboxPage } from './pages/mailinatorPages';

// This used to be a static method of MailInboxPage in C#,
// but in Typescript that creates a circular import reference
export function getInbox(page: Page, mailboxId: string): MailInboxPage {
  return isDev ? new MailDevInboxPage(page, mailboxId) : new MailinatorInboxPage(page, mailboxId);
}

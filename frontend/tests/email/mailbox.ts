import {type EmailSubjects, EmailPage} from './email-page';

import {expect, type Page} from '@playwright/test';

export interface Email {
  body: string;
}

export abstract class Mailbox {
  constructor(readonly email: string) { }

  abstract fetchEmails(subject: EmailSubjects | Omit<string, EmailSubjects>): Promise<Email[]>;

  async openEmail(page: Page, subject: EmailSubjects | Omit<string, EmailSubjects>, index: number = 0): Promise<EmailPage> {
    let email: Email | undefined = undefined;

    await expect.poll(async () => {
      const emails = await this.fetchEmails(subject);
      email = emails[index];
      return email;
    }, {
      intervals: [1_000],
      timeout: 10_000,
      message: `Failed to find email: ${subject}. (Index: ${index})`,
    }).toBeDefined();

    await page.setContent(email!.body);
    return await new EmailPage(page).waitFor();
  }
}

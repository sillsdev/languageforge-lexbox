import type {APIRequestContext} from '@playwright/test';
import {type EmailSubjects} from './email-page';
import {Mailbox, type Email} from './mailbox';

interface MaildevEmail {
  subject: string;
  html: string;
  to: {address: string, name: string}[],
  from: {address: string, name: string}[],
}

export class MaildevMailbox extends Mailbox {
  constructor(readonly email: string, private readonly api: APIRequestContext) {
    super(email);
  }

  async fetchEmails(subject: EmailSubjects | string): Promise<Email[]> {
    const emails = await this.fetchMyEmails();
    return emails.filter(email => email.subject.includes(subject))
      .map(email => ({body: email.html}));
  }

  async fetchMyEmails(): Promise<MaildevEmail[]> {
    // Maildev REST API docs: https://github.com/maildev/maildev/blob/master/docs/rest.md
    const response = await this.api.get(`http://localhost:1080/email`);
    const emails = await response.json() as MaildevEmail[];
    return emails.filter(email => email.to.some(to => to.address === this.email));
  }
}

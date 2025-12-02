import type {APIRequestContext} from '@playwright/test';
import {type EmailSubjects} from './email-page';
import {Mailbox, type Email} from './mailbox';
import {delay} from '$lib/util/time';
import {getErrorMessage} from '$lib/error/utils';

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

  async fetchEmails(subject: EmailSubjects | Omit<string, EmailSubjects>): Promise<Email[]> {
    const emails = await this.fetchMyEmails();
    return emails.filter(email => email.subject.includes(subject as string))
      .map(email => ({body: email.html}));
  }

  async fetchMyEmails(): Promise<MaildevEmail[]> {
    const MAX_TRIES = 3;

    for (let tries = 1; tries <= MAX_TRIES; tries++) {
      try {
        // Maildev REST API docs: https://github.com/maildev/maildev/blob/master/docs/rest.md
        const response = await this.api.get('http://localhost:1080/email');
        const emails = await response.json() as MaildevEmail[];
        return emails.filter(email => email.to.some(to => to.address === this.email));
      } catch (error) {
        const message = getErrorMessage(error);
        console.error(`Error fetching emails, attempt ${tries}/${MAX_TRIES}.`, message);
        if (tries >= MAX_TRIES) throw error;
        await delay(1000);
      }
    }

    // This line should never be reached but TypeScript requires a return statement
    throw new Error('Reached unreachable code');
  }
}

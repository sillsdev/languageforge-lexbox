import {Mailbox, type Email} from './mailbox';

import type {E2EMailboxApi} from './e2e-mailbox-module-patched';
import {type EmailSubjects} from './email-page';

export class E2EMailbox extends Mailbox {

  constructor(
    readonly email: string,
    private readonly e2eMailboxApi: E2EMailboxApi,
  ) {
    super(email);
  }

  async fetchEmails(subject: EmailSubjects | string): Promise<Email[]> {
    const emails = await this.e2eMailboxApi.fetchEmailList()
    return emails.filter(email => email.mail_subject.includes(subject))
      .map(email => ({body: email.mail_body}));
  }
}

import {Mailbox, type Email} from './mailbox';

import type {E2EMailboxApi} from './e2e-mailbox-module-patched';
import type {EmailSubjects} from './email-page';

export class E2EMailbox extends Mailbox {

  constructor(
    readonly email: string,
    private readonly e2eMailboxApi: E2EMailboxApi,
  ) {
    super(email);
  }

  async fetchEmails(subject: EmailSubjects, extraText?: string): Promise<Email[]> {
    const searchText = subject.toString() + (extraText ?? '');
    const emails = await this.e2eMailboxApi.fetchEmailList()
    return emails.filter(email => email.mail_subject.includes(searchText))
      .map(email => ({body: email.mail_body}));
  }
}

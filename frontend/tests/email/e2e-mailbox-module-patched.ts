import E2EMailboxModuleConfused from 'e2e-mailbox';

// The package seems to think it's an ES module, but it's actually a commonJS module.
// https://github.com/allynsweet/E2E-Mailbox/issues/5

// eslint-disable-next-line @typescript-eslint/naming-convention
const E2EMailboxClass = (E2EMailboxModuleConfused as unknown as {
  default: typeof E2EMailboxModuleConfused
}).default;

export class E2EMailboxApi extends E2EMailboxClass { }

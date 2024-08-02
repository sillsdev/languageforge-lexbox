import { type Locator, type Page } from '@playwright/test';
import { BaseComponent } from './baseComponent';

const EMAIL_LABEL = 'Email';
const ROLE_LABEL = 'Role';
const INVITE_LABEL = 'Invite';
const SUBMIT_BUTTON_LABEL = new RegExp(`Add Member|Add or invite Member`);

export class AddMemberModal extends BaseComponent {
  get emailField(): Locator {
    return this.componentLocator.getByLabel(EMAIL_LABEL); // NOT {exact: true}, so it can also match "Email or Send/Receive login"
  }

  get roleField(): Locator {
    return this.componentLocator.getByLabel(ROLE_LABEL, {exact: true});
  }

  get inviteCheckbox(): Locator {
    return this.componentLocator.getByRole('checkbox', {name: INVITE_LABEL});
  }

  get submitButton(): Locator {
    return this.componentLocator.getByRole('button', {name: SUBMIT_BUTTON_LABEL});
  }

  async selectEditorRole(): Promise<void> {
    await this.roleField.selectOption('EDITOR');
  }

  async selectManagerRole(): Promise<void> {
    await this.roleField.selectOption('MANAGER');
  }

  constructor(page: Page) {
    super(page, page.locator('dialog.modal').filter({hasText: 'Add or invite a Member to this project'}));
  }
}

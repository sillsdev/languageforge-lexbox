import { type Locator, type Page } from '@playwright/test';
import { BaseComponent } from './baseComponent';

export class DeleteProjectModal extends BaseComponent {

  get confirmationInput(): Locator {
    return this.componentLocator.getByLabel(`Enter 'DELETE PROJECT' to confirm`);
  }

  get submitButton(): Locator {
    return this.componentLocator.getByRole('button', { name: 'Delete Project' });
  }

  constructor(page: Page) {
    super(page, page.locator(`dialog.modal:has-text("Enter 'DELETE PROJECT' to confirm")`));
  }

  async confirmDeleteProject(): Promise<void> {
    await this.confirmationInput.fill('DELETE PROJECT');
    await this.submitButton.click();
  }
}

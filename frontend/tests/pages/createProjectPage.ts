import { BasePage } from './basePage';
import type { Page } from '@playwright/test';

export class CreateProjectPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', { name: 'Create Project' }), `/project/create`);
  }

  async fillForm(values: { code: string, customCode?: boolean, name?: string, type?: string, purpose?: string, description?: string }): Promise<void> {
    const { code, customCode = false, name = code, type, purpose = 'Software Developer', description = name } = values;
    await this.page.getByLabel('Name').fill(name);
    await this.page.getByLabel('Description').fill(description ?? name);
    if (type) await this.page.getByLabel('Project type').selectOption({ label: type });
    if (purpose) await this.page.getByLabel('Purpose').selectOption({ label: purpose });
    await this.page.getByLabel('Language Code').fill(code);
    if (customCode) {
      await this.page.getByLabel('Custom Code').check();
      await this.page.getByLabel('Code', { exact: true }).fill(code);
    }
  }

  async submit(): Promise<void> {
    await this.page.getByRole('button', {name: 'Create Project'}).click();
  }
}

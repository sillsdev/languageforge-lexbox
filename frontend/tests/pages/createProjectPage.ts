import {BasePage} from './basePage';
import type {Page} from '@playwright/test';

type ProjectConfig = {
  code: string;
  customCode: boolean;
  name: string;
  type: string;
  purpose: 'Software Developer' | 'Testing' | 'Training' | 'Language Project';
  description: string;
};

export class CreateProjectPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', { name: /(Create|Request) Project/ }), `/project/create`);
  }

  async fillForm(values: Pick<ProjectConfig, 'code'> & Partial<ProjectConfig>): Promise<ProjectConfig> {
    let code = values.code;
    const { customCode = false, name = code, type = 'FLEx', purpose = 'Software Developer', description = name } = values;
    await this.page.getByLabel('Name').fill(name);
    await this.page.getByLabel('Description').fill(description ?? name);
    await this.page.getByLabel('Project type').selectOption({ label: type });
    await this.page.getByLabel('Purpose').selectOption({ label: purpose });
    await this.page.getByLabel('Language Code').fill(code);
    if (customCode) {
      await this.page.getByLabel('Custom Code').check();
      await this.page.getByLabel('Code', { exact: true }).fill(code);
    } else {
      code = await this.page.getByLabel('Code', {exact: true}).inputValue();
    }
    return { code, name, type, purpose, description, customCode };
  }

  async submit(): Promise<void> {
    await this.page.getByRole('button', {name: /(Create|Request) Project/}).click();
  }
}

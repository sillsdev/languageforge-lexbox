import {BasePage} from './basePage';
import type {Locator, Page} from '@playwright/test';

type ProjectConfig = {
  code: string;
  customCode: boolean;
  name: string;
  organization: string;
  type: string;
  purpose: 'Software Developer' | 'Testing' | 'Training' | 'Language Project';
  description: string;
};

export class CreateProjectPage extends BasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', { name: /(Create|Request) Project/ }), '/project/create');
  }
  get extraProjectsDiv(): Locator { return this.page.locator('#group-extra-projects'); }
  get askToJoinBtn(): Locator { return this.page.getByRole('button', {name: 'Ask to join', exact: true}); }

  async fillForm(values: Pick<ProjectConfig, 'code'> & Partial<ProjectConfig>): Promise<ProjectConfig> {
    let code = values.code;
    const { customCode = false, name = code, type = 'FieldWorks', purpose = 'Software Developer', description = name, organization = '' } = values;
    await this.page.getByLabel('Name').fill(name);
    await this.page.getByLabel('Description').fill(description ?? name);
    if (organization) await this.page.getByLabel('Organization').selectOption({ label: organization })
    await this.page.getByLabel('Project type').selectOption({ label: type });
    await this.page.getByLabel('Purpose').selectOption({ label: purpose });
    await this.page.getByLabel('Language Code').fill(code);
    if (customCode) {
      await this.page.getByLabel('Custom Code').check();
      await this.page.getByLabel('Code', { exact: true }).fill(code);
    } else {
      code = await this.page.getByLabel('Code', {exact: true}).inputValue();
    }
    return { code, name, organization, type, purpose, description, customCode };
  }

  async selectExtraProject(projectCode: string): Promise<void> {
    await this.extraProjectsDiv.locator(`#extra-projects-${projectCode}`).check();
  }

  async submit(): Promise<void> {
    await this.page.getByRole('button', {name: /(Create|Request) Project/}).click();
  }
}

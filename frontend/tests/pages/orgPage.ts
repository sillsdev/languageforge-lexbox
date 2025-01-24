import type {Locator, Page} from '@playwright/test';

import {BasePage} from './basePage';
import {ProjectPage} from './projectPage';

export class OrgPage extends BasePage {
  get moreSettingsDiv(): Locator { return this.page.locator('.collapse').filter({ hasText: 'More settings' }); }
  get deleteProjectButton(): Locator { return this.moreSettingsDiv.getByRole('button', {name: 'Delete project'}); }

  get projectsTab(): Locator { return this.page.getByRole('tab', { name: 'Projects' }); }
  get membersTab(): Locator { return this.page.getByRole('tab', { name: 'Members' }); }
  get settingsTab(): Locator { return this.page.getByRole('tab', { name: 'Settings' }); }
  get historyTab(): Locator { return this.page.getByRole('tab', { name: 'History' }); }

  projectLink(projectName: string): Locator { return this.page.getByRole('link', {name: projectName, exact: true}); }

  constructor(page: Page, private name: string, private orgId: string) {
    super(page, page.getByRole('heading', {name: `Organization: ${name}`}), `/org/${orgId}`);
  }

  async clickProject(projectName: string): Promise<void> {
    await this.projectsTab.click();
    return this.projectLink(projectName).click();
  }

  async openProject(projectName: string, projectCode: string): Promise<ProjectPage> {
    await this.clickProject(projectName);
    return await new ProjectPage(this.page, projectName, projectCode).waitFor();
  }
}

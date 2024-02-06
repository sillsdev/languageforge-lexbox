import type { Locator, Page } from '@playwright/test';
import { AuthenticatedBasePage } from './authenticatedBasePage';
import { ProjectPage } from './projectPage';

export class AdminDashboardPage extends AuthenticatedBasePage {
  get projectFilterBarInput(): Locator { return this.page.locator('.filter-bar').nth(0).getByRole('textbox'); }

  constructor(page: Page) {
    super(page, page.locator(`.breadcrumbs :text('Admin Dashboard')`), `/admin`);
  }

  async openProject(projectName: string, projectCode: string): Promise<void> {
    await this.clickProject(projectName);
    await new ProjectPage(this.page, projectName, projectCode).waitFor();
  }

  async clickProject(projectName: string): Promise<void> {
    await this.projectFilterBarInput.fill(projectName); // make sure project is visible
    const table = this.page.locator('table').nth(0);
    return table.getByRole('link', {name: projectName, exact: true}).click();
  }
}

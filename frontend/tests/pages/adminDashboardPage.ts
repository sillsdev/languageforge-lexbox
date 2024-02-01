import type { Page } from '@playwright/test';
import { AuthenticatedBasePage } from './authenticatedBasePage';
import { ProjectPage } from './projectPage';

export class AdminDashboardPage extends AuthenticatedBasePage {
  constructor(page: Page) {
    super(page, page.locator(`.breadcrumbs :text('Admin Dashboard')`), `/admin`);
  }

  async openProject(projectName: string, projectCode: string): Promise<void> {
    await this.clickProject(projectName);
    await new ProjectPage(this.page, projectName, projectCode).waitFor();
  }

  clickProject(projectName: string): Promise<void> {
    const table = this.page.locator('table').nth(0);
    return table.getByRole('link', {name: projectName, exact: true}).click();
  }
}

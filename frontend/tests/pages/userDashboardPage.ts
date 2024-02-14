import type { Page } from '@playwright/test';
import { AuthenticatedBasePage } from './authenticatedBasePage';
import { ProjectPage } from './projectPage';

export class UserDashboardPage extends AuthenticatedBasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'My Projects'}), `/`);
  }

  async openProject(projectName: string, projectCode: string): Promise<ProjectPage> {
    const projectHeader = this.page.getByRole('heading', {name: projectName});
    const projectCard = this.page.locator('.card', {has: projectHeader});
    await projectCard.click();
    return new ProjectPage(this.page, projectName, projectCode).waitFor();
  }
}

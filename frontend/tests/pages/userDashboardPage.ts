import {AuthenticatedBasePage} from './authenticatedBasePage';
import {CreateProjectPage} from './createProjectPage';
import type {Page} from '@playwright/test';
import {ProjectPage} from './projectPage';

export class UserDashboardPage extends AuthenticatedBasePage {
  constructor(page: Page) {
    super(page, page.getByRole('heading', {name: 'My Projects'}), '/');
  }

  async openProject(projectName: string, projectCode: string): Promise<ProjectPage> {
    await this.selectGridView();
    const projectHeader = this.page.getByRole('heading', {name: projectName});
    const projectCard = this.page.locator('.card', {has: projectHeader});
    await projectCard.click();
    return new ProjectPage(this.page, projectName, projectCode).waitFor();
  }

  async selectGridView(): Promise<void> {
    await this.page.click('.i-mdi-grid');
  }

  async clickCreateProject(): Promise<CreateProjectPage> {
    await this.page.getByRole('link', {name: /(Create|Request) Project/, exact: true}).click();
    return new CreateProjectPage(this.page).waitFor();
  }
}

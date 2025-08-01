import {expect, type Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';
import {LoginPage} from '../../../../tests/pages/loginPage';

type Server = E2ETestConfig['lexboxServer'];

export class HomePage {


  constructor(private page: Page) {
  }

  public async waitFor() {
    await this.page.waitForLoadState('load');
    await expect(this.page.getByRole('heading', {name: 'Dictionaries'})).toBeVisible();
  }

  public serverSection(server: Server) {
    return this.page.locator(`#${server.hostname}`);
  }

  public userIndicator(server: Server) {
    return this.serverSection(server).locator(`.i-mdi-account-circle`);
  }

  public loginButton(server: Server) {
    return this.serverSection(server).locator(`a:has-text("Login")`);
  }

  public serverProjects(server: Server) {
    return this.serverSection(server).getByRole('row');
  }

  public localProjects() {
    return this.page.locator('#local-projects');
  }

  public async ensureLoggedIn(server: Server, username: string, password: string) {
    await this.serverSection(server).waitFor({state: 'visible'});
    const isLoggedIn = await this.userIndicator(server).isVisible();

    if (isLoggedIn) {
      console.log('User already logged in, skipping login process');
      return;
    }    // Look for login button or link
    const loginButton = this.loginButton(server);

    await loginButton.waitFor({state: 'visible'});
    await loginButton.click();

    await expect(this.page).toHaveURL(url => url.href.startsWith(`${server.protocol}://${server.hostname}/login`));

    const loginPage = new LoginPage(this.page);
    await loginPage.waitFor();
    await loginPage.fillForm(username, password);
    await loginPage.submit();

    await this.userIndicator(server).waitFor({state: 'visible'});
  }

  public async ensureLoggedOut(server: Server) {
    await this.serverSection(server).waitFor({state: 'visible'});
    const isLoggedIn = await this.userIndicator(server).isVisible();

    if (!isLoggedIn) {
      console.log('User already logged out, skipping logout process');
      return;
    }

    await this.userIndicator(server).click();

    const logoutButton = this.page.getByRole('menuitem', {name: 'Logout'});
    await logoutButton.click();

    await this.loginButton(server).waitFor({state: 'visible'});
  }

  async downloadProject(server: Server, projectCode: string) {
    await this.serverProjects(server)
      .locator(`:has-text("${projectCode}")`)
      .first()
      .click();
    await this.page.locator('.i-mdi-loading').waitFor({
      state: 'visible'
    });


    const progressIndicator = this.page.locator('.i-mdi-loading');
    await expect(progressIndicator).toBeVisible();
    await progressIndicator.waitFor({
      state: 'detached',
      timeout: 60_000
    });

    // Look for synced
    const projectElement = this.localProjects().getByText(`${projectCode}`);
    await expect(projectElement).toBeVisible();
  }

  async openLocalProject(projectCode: string) {
    await this.localProjects().getByText(`${projectCode}`).click();
  }
}

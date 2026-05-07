import {expect, type Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';
import {LoginPage} from '../../../../tests/pages/loginPage';

type Server = E2ETestConfig['lexboxServer'];

// FW Lite uses Uri.Authority (host[:port]) as the server.id, so the rendered
// element id always includes the port. Use an attribute selector — `#host:port`
// would parse the `:port` as a CSS pseudo-class.
function serverElementSelector(server: Server) {
  return `[id="${server.hostname}:${server.port}"]`;
}

function serverLoginUrlPrefix(server: Server) {
  return `${server.protocol}://${server.hostname}:${server.port}/login`;
}

export class HomePage {


  constructor(private page: Page) {
  }

  public async waitFor() {
    await this.page.waitForLoadState('load');
    await expect(this.page.getByRole('heading', {name: 'Dictionaries'})).toBeVisible();
  }

  public serverSection(server: Server) {
    return this.page.locator(serverElementSelector(server));
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

    await expect(this.page).toHaveURL(url => url.href.startsWith(serverLoginUrlPrefix(server)));

    const loginPage = new LoginPage(this.page);
    await loginPage.waitFor();
    await loginPage.fillForm(username, password);
    await loginPage.submit();

    // First time a user authorizes a client, lexbox's OpenIddict shows a consent
    // prompt. Fresh kind cluster in CI = always first time. Auto-approved auth
    // skips this step (e.g. local dev with prior consent).
    const authorizeButton = this.page.getByRole('button', {name: /^Authorize\s/i});
    try {
      await authorizeButton.click({timeout: 15000});
    } catch {
      // No consent prompt — already authorized
    }

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
    // FwLite caches the per-server projects list (LexboxProjectService uses
    // IMemoryCache). Page reload doesn't clear it — only the per-server
    // "Refresh Projects" button does (it passes force=true). Click it so the
    // list reflects any server-side changes (e.g. a CRDT sync that just ran).
    const refreshBtn = this.serverSection(server).getByRole('button', {name: 'Refresh Projects'});
    if (await refreshBtn.isVisible().catch(() => false)) {
      await refreshBtn.click();
    }

    const projectRow = this.serverProjects(server).filter({hasText: projectCode}).first();
    await projectRow.waitFor({state: 'visible', timeout: 30_000});
    await projectRow.click();

    // Loading indicator appears while download/sync is in progress
    const progressIndicator = this.page.locator('.i-mdi-loading');
    await progressIndicator.waitFor({state: 'visible', timeout: 30_000});
    await progressIndicator.waitFor({state: 'detached', timeout: 60_000});

    // Verify the project landed in the local section
    await expect(this.localProjects().getByText(projectCode)).toBeVisible();
  }

  async openLocalProject(projectCode: string) {
    await this.localProjects().getByText(`${projectCode}`).click();
  }
}

import {expect, type Page} from '@playwright/test';
import type {E2ETestConfig} from '../types';
import {LoginPage} from '../../../../tests/pages/loginPage';

type Server = E2ETestConfig['lexboxServer'];

// FwLite uses Uri.Authority (host[:port]) as the server.id, so the rendered
// element id always includes the port. Use an attribute selector — `#host:port`
// would parse `:port` as a CSS pseudo-class.
function serverElementSelector(server: Server) {
  return `[id="${server.hostname}:${server.port}"]`;
}

function serverLoginUrlPrefix(server: Server) {
  return `${server.protocol}://${server.hostname}:${server.port}/login`;
}

export class HomePage {
  constructor(private page: Page) {}

  async waitFor() {
    await this.page.waitForLoadState('load');
    await expect(this.page.getByRole('heading', {name: 'Dictionaries'})).toBeVisible();
  }

  serverSection(server: Server) {
    return this.page.locator(serverElementSelector(server));
  }

  userIndicator(server: Server) {
    return this.serverSection(server).locator('.i-mdi-account-circle');
  }

  loginButton(server: Server) {
    return this.serverSection(server).locator('a:has-text("Login")');
  }

  serverProjects(server: Server) {
    return this.serverSection(server).getByRole('row');
  }

  localProjects() {
    return this.page.locator('#local-projects');
  }

  async ensureLoggedIn(server: Server, username: string, password: string) {
    await this.serverSection(server).waitFor({state: 'visible'});
    if (await this.userIndicator(server).isVisible()) return;

    await this.loginButton(server).click();
    await expect(this.page).toHaveURL(url => url.href.startsWith(serverLoginUrlPrefix(server)));

    const loginPage = new LoginPage(this.page);
    await loginPage.waitFor();
    await loginPage.fillForm(username, password);
    await loginPage.submit();

    // First time a user authorizes a client, lexbox's OpenIddict shows a consent
    // prompt. Fresh kind cluster in CI = always first time. Auto-approved auth
    // (e.g. local dev with prior consent) skips this step.
    await this.page.getByRole('button', {name: /^Authorize\s/i})
      .click({timeout: 15_000})
      .catch(() => { /* no consent prompt — already authorized */ });

    await this.userIndicator(server).waitFor({state: 'visible'});
  }

  async ensureLoggedOut(server: Server) {
    await this.serverSection(server).waitFor({state: 'visible'});
    if (!(await this.userIndicator(server).isVisible())) return;

    await this.userIndicator(server).click();
    await this.page.getByRole('menuitem', {name: 'Logout'}).click();
    await this.loginButton(server).waitFor({state: 'visible'});
  }

  async downloadProject(server: Server, projectCode: string) {
    // FwLite caches the per-server projects list (LexboxProjectService uses
    // IMemoryCache). Page reload doesn't clear it — only the per-server
    // "Refresh Projects" button does (it passes force=true).
    const refreshBtn = this.serverSection(server).getByRole('button', {name: 'Refresh Projects'});
    if (await refreshBtn.isVisible().catch(() => false)) await refreshBtn.click();

    const projectRow = this.serverProjects(server).filter({hasText: projectCode}).first();
    await projectRow.waitFor({state: 'visible', timeout: 30_000});
    await projectRow.click();

    const progressIndicator = this.page.locator('.i-mdi-loading');
    await progressIndicator.waitFor({state: 'visible', timeout: 30_000});
    await progressIndicator.waitFor({state: 'detached', timeout: 60_000});

    await expect(this.localProjects().getByText(projectCode)).toBeVisible();
  }

  async openLocalProject(projectCode: string) {
    await this.localProjects().getByText(projectCode).click();
  }
}

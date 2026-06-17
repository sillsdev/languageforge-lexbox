import {expect, type Page} from '@playwright/test';
import {serverUrl, type Server} from '../e2e/config';
// The e2e login flow redirects to the real Lexbox server, which renders the parent
// LexBox web app's login form — so we reuse that app's page object (frontend/tests/).
import {LoginPage} from '../../../tests/pages/loginPage';

// FwLite uses Uri.Authority (host[:port]) as the server.id, so the rendered
// element id always includes the port. Use an attribute selector — `#host:port`
// would parse `:port` as a CSS pseudo-class.
function serverElementSelector(s: Server): string {
  return `[id="${s.hostname}:${s.port}"]`;
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

  // Only downloaded (local) projects render as anchors to `/project/{code}`;
  // server-side projects appear as rows inside their server section.
  localProjectLink(projectCode: string) {
    return this.page.locator(`a[href="/project/${projectCode}"]`);
  }

  async ensureLoggedIn(server: Server, username: string, password: string) {
    await this.serverSection(server).waitFor({state: 'visible'});
    if (await this.userIndicator(server).isVisible()) return;

    await this.loginButton(server).click();
    await expect(this.page).toHaveURL(url => url.href.startsWith(`${serverUrl(server)}/login`));

    const loginPage = new LoginPage(this.page);
    await loginPage.waitFor();
    await loginPage.fillForm(username, password);
    await loginPage.submit();

    // First-time consent prompt (OpenIddict). Fresh kind cluster in CI = always shown;
    // re-runs with prior consent skip it.
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
    // Refresh the list to ensure any newly-available project appears
    const refreshBtn = this.serverSection(server).getByRole('button', {name: 'Refresh Projects'});
    if (await refreshBtn.isVisible().catch(() => false)) await refreshBtn.click();

    const projectRow = this.serverProjects(server).filter({hasText: projectCode}).first();
    await projectRow.waitFor({state: 'visible', timeout: 30_000});
    await projectRow.click();

    const progressIndicator = this.page.locator('.i-mdi-loading');
    await progressIndicator.waitFor({state: 'visible', timeout: 30_000});
    await progressIndicator.waitFor({state: 'detached', timeout: 60_000});

    await expect(this.localProjectLink(projectCode)).toBeVisible();
  }

  async openLocalProject(projectCode: string) {
    await this.localProjectLink(projectCode).click();
  }
}

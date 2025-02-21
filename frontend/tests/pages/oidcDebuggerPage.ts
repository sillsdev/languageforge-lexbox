import {BasePage} from './basePage';
import type {Page} from '@playwright/test';

export class OidcDebuggerPage extends BasePage {
static readonly redirectUrl = 'https://oidcdebugger.com/debug';
  debuggerPage: BasePage = new BasePage(this.page, this.page.getByText('Success!'), OidcDebuggerPage.redirectUrl);
  constructor(page: Page) {
    super(page, page.getByText('OpenID Connect Debugger'), 'https://oidcdebugger.com');
  }

  async fillForm(baseURL: string) {
    await this.page.locator('#authorizeUri').fill(`${baseURL}/api/oauth/open-id-auth`);
    await this.page.locator('#redirectUri').fill(OidcDebuggerPage.redirectUrl);
    await this.page.locator('#clientId').fill(`oidc-debugger`);
    await this.page.locator('#scopes').fill(`openid profile`);
    await this.page.locator('#responseType-code').check();
    await this.page.locator('#use-pkce').check();
    await this.page.locator('#tokenUri').fill(`${baseURL}/api/oauth/token`);
    await this.page.locator('#responseMode-formPost').check();
  }

  async submit(): Promise<void> {
    await this.page.getByRole('link', {name: 'Send Request'}).click();
  }

  async waitForDebuggerPage() {
    await this.debuggerPage.waitFor();
  }

  async getDebuggerIdToken(): Promise<string> {
    return await this.debuggerPage.page.getByTitle('PKCE result').innerText();
  }
}

import {BasePage} from './basePage';
import {expect, type Page} from '@playwright/test';

export class OidcDebuggerPage extends BasePage {
  static readonly redirectUrl = 'https://oidcdebugger.com/debug';
  static readonly clientId = 'oidc-debugger';
  static readonly scopes = 'openid profile';

  readonly successPage = new OidcDebuggerSuccessPage(this.page);
  constructor(page: Page) {
    super(page, page.getByText('OpenID Connect Debugger'), 'https://oidcdebugger.com');
  }

  async fillForm(baseURL: string): Promise<void> {
    await this.page.locator('#authorizeUri').fill(`${baseURL}/api/oauth/open-id-auth`);
    await this.page.locator('#redirectUri').fill(OidcDebuggerPage.redirectUrl);
    await this.page.locator('#clientId').fill(OidcDebuggerPage.clientId);
    await this.page.locator('#scopes').fill(OidcDebuggerPage.scopes);
    await this.page.locator('#responseType-code').check();
    await this.page.locator('#use-pkce').check();
    await this.page.locator('#tokenUri').fill(`${baseURL}/api/oauth/token`);
    await this.page.locator('#responseMode-formPost').check();
  }

  async submit(): Promise<void> {
    await this.page.getByRole('link', {name: 'Send Request'}).click();
  }

  async waitForSuccessPage(): Promise<OidcDebuggerSuccessPage> {
    return await this.successPage.waitFor();
  }
}

export class OidcDebuggerSuccessPage extends BasePage {

  constructor(page: Page) {
    super(page, page.getByText('Success!'), OidcDebuggerPage.redirectUrl);
  }

  async getPkceResult(): Promise<Record<string, string>> {
    const str = await this.page.getByTitle('PKCE result').innerText();
    return Object.fromEntries<string>(str.split('\n').map(s => s.split('=', 2) as [string, string]));
  }

  async getDebuggerAccessToken(): Promise<string> {
    const result = await this.getPkceResult();
    expect(result.access_token).not.toBeFalsy();
    return result.access_token;
  }

  async getDebuggerIdToken(): Promise<string> {
    const result = await this.getPkceResult();
    expect(result.id_token).not.toBeFalsy();
    return result.id_token;
  }
}

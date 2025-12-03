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

  async fillForm(baseURL: string): Promise<{codeVerifier: string}> {
    await this.page.locator('#authorizeUri').fill(`${baseURL}/api/oauth/open-id-auth`);
    await this.page.locator('#redirectUri').fill(OidcDebuggerPage.redirectUrl);
    await this.page.locator('#clientId').fill(OidcDebuggerPage.clientId);
    await this.page.locator('#scopes').fill(OidcDebuggerPage.scopes);
    await this.page.locator('#responseType-code').check();
    await this.page.locator('#use-pkce').check();
    await this.page.locator('#tokenUri').fill(`${baseURL}/api/oauth/token`);
    await this.page.locator('#responseMode-formPost').check();
    const codeVerifier = await this.page.locator('#pkce-code-verifier').inputValue();
    return { codeVerifier };
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

  async getPkceResult(codeVerifier: string): Promise<Record<string, string>> {
    const browserManagedResult = await this.tryGetPkceResultFromPage();
    if (browserManagedResult) return browserManagedResult;

    // the web page tried and failed to do the following automatically, so we'll do it.
    // (This now happens in Chrome due to not allowing a reuqest from https to http)
    const {code, issuer} = await this.extractAuthorizationResponse();
    const tokenUrl = issuer.replace(/\/+$/, '') + '/api/oauth/token';

    const tokenResponse = await this.page.request.post(tokenUrl, {
      form: {
        /* eslint-disable @typescript-eslint/naming-convention */
        grant_type: 'authorization_code',
        code,
        code_verifier: codeVerifier,
        redirect_uri: OidcDebuggerPage.redirectUrl,
        client_id: OidcDebuggerPage.clientId,
        /* eslint-enable @typescript-eslint/naming-convention */
      },
    });

    if (!tokenResponse.ok()) {
      const body = await tokenResponse.text();
      console.error(body);
      throw new Error(`Token request failed (${tokenResponse.status()}): ${body}`);
    }

    return await tokenResponse.json() as Record<string, string>;
  }

  private async tryGetPkceResultFromPage(): Promise<Record<string, string> | undefined> {
    const tokenContainer = this.page.getByTitle('PKCE result');
    if (!await tokenContainer.isVisible()) return;
    const tokenString = await tokenContainer.innerText();
    return Object.fromEntries<string>(tokenString.split('\n').map(s => s.split('=', 2) as [string, string]));
  }

  private async extractAuthorizationResponse(): Promise<{code: string; issuer: string}> {
    const formValues = await this.page.getByTitle('Form body values').innerText();
    const code = formValues.match(/code=([^\n]+)/)?.[1];
    const issuer = formValues.match(/iss=([^\n]+)/)?.[1];

    if (!code || !issuer) {
      throw new Error('Missing authorization code or issuer');
    }

    return {code, issuer};
  }

  async getDebuggerAccessToken(codeVerifier: string): Promise<string> {
    const result = await this.getPkceResult(codeVerifier);
    expect(result.access_token).not.toBeFalsy();
    return result.access_token;
  }

  async getDebuggerIdToken(codeVerifier: string): Promise<string> {
    const result = await this.getPkceResult(codeVerifier);
    expect(result.id_token).not.toBeFalsy();
    return result.id_token;
  }
}

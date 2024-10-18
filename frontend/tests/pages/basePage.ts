import { expect, type Locator, type Page } from '@playwright/test';

// Javascript doesn't have Regex.Escape() so we have to roll our own
// Implementation from https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_Expressions
function regexEscape(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}

export class BasePage {
  readonly locators: Locator[];

  protected get locatorTimeout(): number | undefined {
    return undefined;
  }

  get urlPattern(): RegExp | undefined {
    if (!this.url) return undefined;
    return new RegExp(regexEscape(this.url) + '($|\\?|#)');
  }

  constructor(readonly page: Page, locator: Locator | Locator[], protected url?: string) {
    if (Array.isArray(locator)) {
      this.locators = locator;
    } else {
      this.locators = [locator];
    }
  }

  async goto({ expectRedirect, expectErrorResponse, urlEnd }: {expectRedirect?: boolean, expectErrorResponse?: boolean, urlEnd?: string } = {}): Promise<this> {
    if (!this.url) {
        throw new Error('Can\'t explicitly navigate to page, because it doesn\'t have a configured url.');
    }

    const response = await this.page.goto(this.url + (urlEnd ?? ''));
    // response is null if same URL, but different hash - and that's okay
    if (response) {
      if (expectErrorResponse) {
        expect(response.ok()).toBeFalsy();
      } else {
        expect(response.ok()).toBeTruthy();
      }
    }
    if (!expectRedirect && !expectErrorResponse) {
      await this.waitFor();
    }
    return this;
  }

  async waitFor(): Promise<this> {
    if (!this.urlPattern) {
      await this.page.waitForLoadState('load');
    } else {
      // first use expect() so we get a good error message
      await expect(this.page).toHaveURL(this.urlPattern, {timeout: 10_000});
      // still wait to ensure we reach the state we expect
      await this.page.waitForURL(this.urlPattern, {waitUntil: 'load'});
    }
    await BasePage.waitForHydration(this.page); // wait for, e.g., onclick handlers to be attached
    await Promise.all(this.locators.map(l => expect(l).toBeVisible({ timeout: this.locatorTimeout })));
    return this;
  }

  static async waitForHydration(page: Page): Promise<void> {
    await page.locator('.hydrating').waitFor({state: 'detached'});
  }
}

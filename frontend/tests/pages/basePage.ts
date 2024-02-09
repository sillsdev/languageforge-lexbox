import { expect, type Locator, type Page } from '@playwright/test';

// Javascript doesn't have Regex.Escape() so we have to roll our own
// Implementation from https://developer.mozilla.org/en-US/docs/Web/JavaScript/Guide/Regular_Expressions
function regexEscape(s: string): string {
  return s.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'); // $& means the whole matched string
}

export class BasePage {
  readonly page: Page;
  protected url?: string;
  readonly locators: Locator[];
  get urlPattern(): RegExp | undefined {
    if (this.url == null) return undefined;
    return new RegExp(regexEscape(this.url) + '($|\\?|#)');
  }

  constructor(page: Page, locator: Locator | Locator[], url?: string) {
    this.page = page;
    this.url = url;
    if (Array.isArray(locator)) {
      this.locators = locator;
    } else {
      this.locators = [locator];
    }
  }

  async goto({ expectRedirect }: {expectRedirect: boolean} = {expectRedirect: false}): Promise<this> {
    if (this.url == undefined) {
        throw new Error('Can\'t explicitly navigate to page, because it doesn\'t have a configured url.');
    }

    const response = await this.page.goto(this.url);
    // response is null if same URL, but different hash - and that's okay
    if (response != null) {
      expect(response.ok()).toBeTruthy();
    }
    if (!expectRedirect) {
      await this.waitFor();
    }
    return this;
  }

  async waitFor(): Promise<this> {
    if (this.urlPattern == null) {
      await this.page.waitForLoadState('load');
    } else {
      // first use expect() so we get a good error message
      await expect(this.page).toHaveURL(this.urlPattern, {timeout: 10_000});
      // still wait to ensure we reach the state we expect
      await this.page.waitForURL(this.urlPattern, {waitUntil: 'load'});
    }
    await BasePage.waitForHydration(this.page); // wait for, e.g., onclick handlers to be attached
    await Promise.all(this.locators.map(l => expect(l).toBeVisible()));
    return this;
  }

  static async waitForHydration(page: Page): Promise<void> {
    await page.locator('.hydrating').waitFor({state: 'detached'});
  }
}

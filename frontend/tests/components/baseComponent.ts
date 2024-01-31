import { expect, type Locator, type Page } from '@playwright/test';

export class BaseComponent {
  readonly page: Page;
  readonly componentLocator: Locator;

  constructor(page: Page, locator: Locator)
  {
    this.page = page;
    this.componentLocator = locator;
  }

  async waitFor(): Promise<this>
  {
    await this.componentLocator.waitFor();
    return this;
  }

  // TODO: Get rid of this method. The encapsulation is not worth the ugliness of having to copy .locator()'s options type (since Playwright doesn't expose a type for it)
  // Instead, we'll just expose a `.locator` getter that returns componentLocator or something.
  locator(selector: string | Locator, options: { has?: Locator | undefined; hasNot?: Locator | undefined; hasNotText?: string | RegExp | undefined; hasText?: string | RegExp | undefined; } | undefined): Locator {
    return this.componentLocator.locator(selector, options);
  }

  async assertGone(): Promise<void>
  {
    await expect(this.componentLocator).not.toBeAttached();
  }
}

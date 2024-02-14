import { expect, type Locator, type Page } from '@playwright/test';

export class BaseComponent {
  readonly page: Page;
  readonly componentLocator: Locator;

  constructor(page: Page, locator: Locator) {
    this.page = page;
    this.componentLocator = locator;
  }

  async waitFor(): Promise<this> {
    await this.componentLocator.waitFor();
    return this;
  }

  async assertGone(): Promise<void> {
    await expect(this.componentLocator).not.toBeAttached();
  }
}

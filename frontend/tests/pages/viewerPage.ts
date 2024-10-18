import type { Locator, Page } from '@playwright/test';

import { BasePage } from './basePage';

export class ViewerPage extends BasePage {

  protected override get locatorTimeout(): undefined | number {
    return 10_000; // the viewer can take a while to load a project
  }

  get entryListItems(): Locator {
    return this.page.locator('.entry-list .entry');
  }

  get searchInputButton(): Locator {
    return this.page.locator('.AppBar')
      .getByRole('button').filter({ hasText: 'Find entry...' });
  }

  get searchResults(): Locator {
    return this.page.locator('.Dialog')
      .locator('.ListItem');
  }

  get entryDictionaryPreview(): Locator {
    return this.page.locator('.fancy-border');
  }

  constructor(page: Page, name: string, code: string) {
    super(page, page.getByRole('heading', { name }), `/project/${code}/viewer`);
  }

  async dismissAboutDialog(): Promise<void> {
    await this.page.locator('.Dialog', { hasText: 'What is this?' })
      .getByRole('button', { name: 'Close' })
      .click();
  }

  async search(search: string): Promise<void> {
    await this.searchInputButton.click();
    await this.page.locator('.Dialog').getByRole('textbox').fill(search);
  }

  async clickSearchResult(result: string): Promise<void> {
    await this.searchResults
      .filter({ hasText: result })
      .click();
  }
}

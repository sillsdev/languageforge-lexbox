import {expect, type Page} from '@playwright/test';

export class ProjectPage {
  constructor(private page: Page, private projectCode: string) {}

  async waitFor() {
    await this.page.waitForLoadState('load');
    await this.page.locator('.i-mdi-loading').waitFor({state: 'detached'});
    await expect(this.page.locator('.animate-pulse')).toHaveCount(0);
    await expect(this.page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
    await expect(this.page.getByRole('button', {name: 'Headword'})).toBeVisible();
    // Entries hydrate after the table shell renders, so poll instead of one-shotting.
    await expect.poll(() => this.entryRows().count()).toBeGreaterThan(5);
  }

  entryRows() {
    return this.page.getByRole('table').getByRole('row');
  }
}

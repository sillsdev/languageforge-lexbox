import {expect, type Page} from '@playwright/test';

export class ProjectPage {
  constructor(private page: Page, private projectCode: string) {

  }

  public async waitFor() {
    await this.page.waitForLoadState('load');
    await this.page.locator('.i-mdi-loading').waitFor({state: 'detached'});
    await expect(this.page.locator('.animate-pulse')).toHaveCount(0);
    await expect(this.page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
    await expect(this.page.getByRole('button', {name: 'Headword'})).toBeVisible();
    const count = await this.entryRows().count();
    expect(count).toBeGreaterThan(5);
  }

  public entryRows() {
    return this.page.getByRole('table').getByRole('row');
  }
}

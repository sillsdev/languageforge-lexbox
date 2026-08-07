import {expect, type Page} from '@playwright/test';
import {EntriesListComponent} from './entries-list.component';
import {EntryViewComponent} from './entry-view.component';

/**
 * The project's browse view: entries list on the left, entry detail/edit on the right.
 * Used by both the in-memory-demo UI tests and the real-backend e2e tests.
 */
export class ProjectPage {
  readonly entriesList: EntriesListComponent;
  readonly entryView: EntryViewComponent;

  constructor(readonly page: Page) {
    this.entriesList = new EntriesListComponent(page);
    this.entryView = new EntryViewComponent(page);
  }

  async waitFor() {
    await this.page.waitForLoadState('load');
    await this.page.locator('.i-mdi-loading').waitFor({state: 'detached'});
    await this.page.waitForFunction(() => document.fonts.ready);
    await expect(this.page.locator('.animate-pulse')).toHaveCount(0);
    await expect(this.entriesList.skeletons).toHaveCount(0);
    await expect(this.page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
    await expect(this.page.getByRole('button', {name: 'Headword'})).toBeVisible();
    // Entries hydrate after the table shell renders, so poll instead of one-shotting.
    await expect.poll(() => this.entriesList.entryRows.count()).toBeGreaterThan(5);
  }

  async selectEntryByFilter(filter: string) {
    await this.entriesList.filterByText(filter);
    await this.entriesList.entryWithText(filter).click();
    await this.entryView.waitForEntryLoaded();
  }
}

import {type Page} from '@playwright/test';
import {waitForProjectViewReady} from './test-utils';
import {EntriesListComponent} from './entries-list-component';
import {EntryViewComponent} from './entry-view-component';
import {EntryApiHelper} from './entry-api-helper';

/**
 * Page object for the browse page which contains:
 * - EntriesListComponent (left panel - entry list with virtual scrolling)
 * - EntryViewComponent (right panel - entry detail/edit view)
 */
export class BrowsePage {
  readonly entriesList: EntriesListComponent;
  readonly entryView: EntryViewComponent;
  readonly api: EntryApiHelper;

  constructor(readonly page: Page) {
    this.api = new EntryApiHelper(page);
    this.entriesList = new EntriesListComponent(page, this.api);
    this.entryView = new EntryViewComponent(page);
  }

  async goto(): Promise<void> {
    await this.page.goto('/testing/project-view');
    await waitForProjectViewReady(this.page, true);
  }

  async selectEntryByFilter(filter: string): Promise<void> {
    await this.entriesList.filterByText(filter);
    await this.entriesList.entryWithText(filter.slice(0, 5)).click();
    await this.entryView.waitForEntryLoaded();
  }
}

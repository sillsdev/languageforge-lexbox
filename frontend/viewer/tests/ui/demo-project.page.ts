import {type Page} from '@playwright/test';
import {ProjectPage} from '../pages/project.page';
import {EntryApiHelper} from './entry-api-helper';

/**
 * ProjectPage variant for the in-memory demo served by `vite dev` at
 * `/testing/project-view`. Exposes the demo's `__PLAYWRIGHT_UTILS__.demoApi`
 * bridge via `api`, and adds api-aware helpers (e.g. `scrollToIndex`) that
 * can't be expressed against UI alone.
 */
export class DemoProjectPage extends ProjectPage {
  readonly api: EntryApiHelper;

  constructor(page: Page) {
    super(page);
    this.api = new EntryApiHelper(page);
  }

  async goto() {
    await this.page.goto('/testing/project-view');
    await this.waitFor();
    // The demo exposes its in-memory API on `window` for tests; api calls would race without this.
    await this.page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi, {timeout: 5000});
  }

  /**
   * Scroll the virtual list to a target entry index. Requires the demo api to
   * report the total entry count so we can convert index → scroll position.
   */
  async scrollToIndex(targetIndex: number): Promise<void> {
    const totalCount = await this.api.countEntries();
    if (targetIndex >= totalCount) throw new Error(`Target index ${targetIndex} exceeds total count ${totalCount}`);

    const targetScroll = await this.entriesList.vlist.evaluate((el, params) => {
      const {idx, total} = params;
      return (idx / total) * el.scrollHeight;
    }, {idx: targetIndex, total: totalCount});

    await this.entriesList.vlist.evaluate((el, target) => {
      el.scrollTop = Math.min(target, el.scrollHeight - el.clientHeight);
    }, targetScroll);

    await this.page.waitForTimeout(300);
    await this.entriesList.waitForSkeletonsToResolve();
  }
}

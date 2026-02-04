import {type Locator, type Page, expect} from '@playwright/test';
import {waitForProjectViewReady} from './test-utils';
import type {EntryApiHelper} from './entry-api-helper';

/**
 * Component object for EntriesList.
 * Provides locators and common actions for virtual scrolling entry list.
 */
export class EntriesListComponent {
  readonly vlist: Locator;
  readonly entryRows: Locator;
  readonly skeletons: Locator;
  readonly selectedEntry: Locator;
  readonly searchInput: Locator;

  constructor(readonly page: Page, readonly api: EntryApiHelper) {
    const table = page.locator('[role="table"]');
    this.vlist = table.locator('> div > div');
    this.entryRows = table.locator('[role="row"]');
    this.skeletons = table.locator('[data-skeleton]');
    this.selectedEntry = table.locator('[role="row"][aria-selected="true"]');
    this.searchInput = page.locator('input.real-input').first();
  }

  async goto(waitForTestUtils = false): Promise<void> {
    await this.page.goto('/testing/project-view');
    await waitForProjectViewReady(this.page, waitForTestUtils);
  }

  async waitForSkeletonsToResolve(): Promise<void> {
    await expect(this.skeletons).toHaveCount(0, {timeout: 5000});
  }

  async getVisibleEntryTexts(maxCount = 10): Promise<string[]> {
    const texts: string[] = [];
    const count = await this.entryRows.count();
    for (let i = 0; i < Math.min(count, maxCount); i++) {
      const text = await this.entryRows.nth(i).textContent();
      if (text) texts.push(text.trim());
    }
    return texts;
  }

  async measureItemHeight(): Promise<number> {
    await expect(this.entryRows.first()).toBeVisible();
    const firstBox = await this.entryRows.first().boundingBox();
    const secondBox = await this.entryRows.nth(1).boundingBox();
    if (!firstBox || !secondBox) throw new Error('Could not measure entry rows');
    return secondBox.y - firstBox.y;
  }

  async scrollToPixels(pixels: number): Promise<void> {
    await this.vlist.evaluate((el, target) => {
      el.scrollTop = target;
    }, pixels);
    await this.page.waitForTimeout(300);
  }

  async scrollToPercent(percent: number): Promise<void> {
    const scrollHeight = await this.vlist.evaluate((el) => el.scrollHeight);
    await this.scrollToPixels(scrollHeight * percent);
  }

  async scrollToIndex(targetIndex: number): Promise<void> {
    const totalCount = await this.api.countEntries();
    if (targetIndex >= totalCount) throw new Error(`Target index ${targetIndex} exceeds total count ${totalCount}`);

    const targetScroll = await this.vlist.evaluate((el, params) => {
      const {idx, total} = params;
      return (idx / total) * el.scrollHeight;
    }, {idx: targetIndex, total: totalCount});

    await this.vlist.evaluate((el, target) => {
      el.scrollTop = Math.min(target, el.scrollHeight - el.clientHeight);
    }, targetScroll);

    await this.page.waitForTimeout(300);
    await this.waitForSkeletonsToResolve();
  }

  async scrollToEnd(): Promise<void> {
    await this.vlist.evaluate((el) => {
      el.scrollTop = el.scrollHeight;
    });
    await this.page.waitForTimeout(500);
    await this.waitForSkeletonsToResolve();
  }

  async getScrollHeight(): Promise<number> {
    return this.vlist.evaluate((el) => el.scrollHeight);
  }

  async getScrollTop(): Promise<number> {
    return this.vlist.evaluate((el) => el.scrollTop);
  }

  async selectEntryByIndex(index: number): Promise<string> {
    const entry = this.entryRows.nth(index);
    await expect(entry).toBeInViewport();
    const text = await entry.textContent();
    await entry.click();
    return text?.trim() ?? '';
  }

  async filterByText(text: string): Promise<void> {
    await this.searchInput.fill(text);
    await this.page.waitForTimeout(500);
    await this.waitForSkeletonsToResolve();
  }

  async clearFilter(): Promise<void> {
    await this.searchInput.clear();
    await this.page.waitForTimeout(500);
    await this.waitForSkeletonsToResolve();
  }

  entryWithText(text: string): Locator {
    return this.entryRows.filter({hasText: text}).first();
  }
}

import {expect, test} from '@playwright/test';

import {EntryApiHelper} from './entry-api-helper';

type CountSpy = {statsFilters: Set<string>, filteredCounts: number, left: boolean, countsAfterLeaving: number};

// The task and dashboard stats are cached per project, but each filtered count scans every entry, so only an open
// view may recount them. A subscription made in the cached factory instead dies with the first view that opened it.
test('stats views count live only while open, also after a revisit; the sidebar count always follows', async ({page}) => {
  // Not Browse: its entry list runs filtered counts too, and the spy must only learn the stats views' filters.
  await page.goto('/testing/project-view/tasks');
  await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi);
  const api = new EntryApiHelper(page);

  const browseNav = page.getByRole('button', {name: 'Browse'});
  const taskCount = page.locator('[data-task-id="missing-part-of-speech"]');
  const totalEntries = page.getByTestId('total-entries');
  function digits(text: string | null): number {
    return parseInt((text ?? '').replace(/\D/g, ''), 10);
  }
  async function readTotal() {
    return digits(await totalEntries.textContent());
  }
  async function readSidebarCount() {
    return digits(await browseNav.textContent());
  }
  await expect(taskCount).toContainText(/\d+ (entries|words) to go/);

  await page.evaluate(() => {
    const demoApi = window.__PLAYWRIGHT_UTILS__.demoApi;
    const countEntries = demoApi.countEntries.bind(demoApi);
    const spy: CountSpy = {statsFilters: new Set(), filteredCounts: 0, left: false, countsAfterLeaving: 0};
    (window as unknown as {countSpy: CountSpy}).countSpy = spy;
    demoApi.countEntries = (query, options) => {
      const filter = options?.filter?.gridifyFilter;
      if (filter) spy.filteredCounts++;
      if (filter && !spy.left) spy.statsFilters.add(filter);
      if (filter && spy.left && spy.statsFilters.has(filter)) spy.countsAfterLeaving++;
      return countEntries(query, options);
    };
  });
  function readSpy() {
    return page.evaluate(() => {
      const {filteredCounts, countsAfterLeaving} = (window as unknown as {countSpy: CountSpy}).countSpy;
      return {filteredCounts, countsAfterLeaving};
    });
  }

  await page.getByRole('button', {name: 'Dashboard'}).click();
  await expect(totalEntries).toHaveText(/\d/);
  const before = await readTotal();
  await api.createEntryWithHeadword('qstatslive');
  await expect.poll(readTotal).toBe(before + 1);

  // Entering recounts every task, so the spy learns their filters. Let that finish before leaving.
  await page.getByRole('button', {name: 'Tasks'}).click();
  await expect(taskCount).toBeVisible();
  await expect.poll(async () => {
    const {filteredCounts} = await readSpy();
    await page.waitForTimeout(300);
    return (await readSpy()).filteredCounts - filteredCounts;
  }).toBe(0);
  await page.evaluate(() => { (window as unknown as {countSpy: CountSpy}).countSpy.left = true; });
  await browseNav.click();
  await expect(taskCount).toBeHidden();

  await api.createEntryWithHeadword('qstatsgone');
  // The sidebar count proves the change was announced; then give the views' debounce time to fire.
  await expect.poll(readSidebarCount).toBe(before + 2);
  await page.waitForTimeout(700);
  expect((await readSpy()).countsAfterLeaving).toBe(0);

  // Opening the dashboard recounts anyway, so only a change made while it's open shows it subscribed again.
  await page.getByRole('button', {name: 'Dashboard'}).click();
  await expect.poll(readTotal).toBe(before + 2);
  await api.createEntryWithHeadword('qstatsback');
  await expect.poll(readTotal).toBe(before + 3);
});

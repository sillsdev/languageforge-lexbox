import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

// The stats are cached per project but only kept up to date while the dashboard is mounted,
// so it has to catch up on changes made elsewhere when it's opened again.
test.describe('Dashboard stats', () => {

  test('total entries count is current after an entry is deleted in another view', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    const dashboardNav = page.getByRole('button', {name: 'Dashboard'});
    const browseNav = page.getByRole('button', {name: 'Browse'});
    const totalEntries = page.locator('[class*="bg-primary/5"] [data-slot="card-title"]');

    async function readTotal() {
      return parseInt((await totalEntries.innerText()).replace(/\D/g, ''), 10);
    }

    await dashboardNav.click();
    await expect(totalEntries).toHaveText(/\d/);
    const initialCount = await projectPage.api.countEntries();
    expect(await readTotal()).toBe(initialCount);

    await browseNav.click();
    await expect(totalEntries).toBeHidden();
    await projectPage.api.deleteEntry(await projectPage.api.getEntryIdAtIndex(0));

    await dashboardNav.click();
    await expect.poll(readTotal).toBe(initialCount - 1);
  });
});

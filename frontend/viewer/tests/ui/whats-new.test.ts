import {expect, test} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';
import {assertScreenshotInBothColorSchemes} from '../helpers/argos-screenshot';
import {WHATS_NEW_VERSION} from '../../src/lib/whats-new/whats-new';

const toast = 'FieldWorks Lite has been updated.';

test("what's new opens from the sidebar", async ({page}) => {
  await new DemoProjectPage(page).goto();
  await expect(page.getByText(toast)).toHaveCount(0);

  await page.getByRole('button', {name: 'Help & More'}).click();
  await page.getByRole('button', {name: "What's new"}).click();

  await expect(page.getByRole('dialog', {name: "What's new in FieldWorks Lite"})).toBeVisible();
  await assertScreenshotInBothColorSchemes(page, 'whats-new-dialog');
});

test("what's new is announced once after an update", async ({page}) => {
  // Only on the first load: the script runs again on reload, which must see what the app stored
  await page.addInitScript(() => {
    if (sessionStorage.getItem('seeded')) return;
    sessionStorage.setItem('seeded', '1');
    localStorage.setItem('WhatsNewSeenVersion', '2000-01-01');
  });
  await new DemoProjectPage(page).goto();

  await expect(page.getByText(toast)).toBeVisible();
  await assertScreenshotInBothColorSchemes(page, 'whats-new-toast');
  await page.getByRole('button', {name: "What's new"}).click();
  await expect(page.getByRole('dialog', {name: "What's new in FieldWorks Lite"})).toBeVisible();
  await expect(page.getByText(toast)).toHaveCount(0);

  // The version is marked seen when the toast shows, so a second launch is quiet
  expect(await page.evaluate(() => localStorage.getItem('WhatsNewSeenVersion'))).toBe(WHATS_NEW_VERSION);
  await page.reload();
  await new DemoProjectPage(page).waitFor();
  await expect(page.getByText(toast)).toHaveCount(0);
});

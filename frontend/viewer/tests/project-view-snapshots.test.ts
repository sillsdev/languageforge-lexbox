﻿import {type Page, expect, test} from '@playwright/test';
import { assertSnapshot } from './snapshot';

const viewports = [
  {height: 720, width: 1280, name: 'medium'},
  {height: 812, width: 375, name: 'iphone-11'},
];

  for (const colorScheme of ['light', 'dark'] as const) {
    test(`ui snapshot selected entry (color: ${colorScheme})`, async ({page}) => {
      await page.emulateMedia({colorScheme});
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);
      await page.getByRole('row', {name: 'ambuka to cross a body of'}).click();
      await expect(page.getByRole('heading', {name: 'ambuka'})).toBeVisible();
      await expect(page.locator('.i-mdi-dots-vertical')).toBeVisible();
      await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
      await assertSnapshot(page, 'project-view-entry-selected-' + colorScheme);
    });

    test(`ui snapshot default (color: ${colorScheme})`, async ({page}) => {
      await page.emulateMedia({colorScheme});
      await page.goto('/testing/project-view');
      await waitForProjectViewReady(page);
      await expect(page.locator('.animate-pulse')).toHaveCount(0);
      await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
      await expect(page.getByRole('textbox', {name: 'Filter'})).toBeVisible();
      await assertSnapshot(page, 'project-view-default-' + colorScheme);
    });
  }


async function waitForProjectViewReady(page: Page) {
  await expect(page.locator('.i-mdi-loading')).toHaveCount(0);
  await page.waitForFunction(() => document.fonts.ready);
  await expect(page.locator('.animate-pulse')).toHaveCount(0);
}

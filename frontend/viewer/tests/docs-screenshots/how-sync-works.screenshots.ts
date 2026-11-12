import {expect, test} from '@playwright/test';
import {blurFocusedField, docsScreenshot, openStory} from './docs-screenshot';

// docs/site/fw-lite/how-sync-works.mdx: the two tabs of the Synchronize dialog.

test('FieldWorks Lite tab', async ({page}) => {
  await openStory(page, 'sync-sync-dialog--pending');
  const dialog = page.getByRole('dialog');
  await expect(dialog.getByText('Pending')).toBeVisible();
  await blurFocusedField(page);
  await docsScreenshot(page, 'sync-fw-lite-tab', dialog);
});

test('Lexbox tab', async ({page}) => {
  await openStory(page, 'sync-sync-dialog--pending');
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('tab', {name: 'Lexbox'}).click();
  await expect(dialog.getByRole('button', {name: 'Sync', exact: true})).toBeVisible();
  await blurFocusedField(page);
  await docsScreenshot(page, 'sync-lexbox-tab', dialog);
});

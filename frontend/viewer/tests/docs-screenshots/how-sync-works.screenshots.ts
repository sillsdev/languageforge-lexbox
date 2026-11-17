import {expect, test} from '@playwright/test';
import {blurFocusedField, docsScreenshot, openStory} from './docs-screenshot';

// docs/site/fw-lite/how-sync-works.mdx: the Sync Changes dialog: the "Other FieldWorks Lite users" and
// "FieldWorks Send/Receive (on Lexbox)" method views (file names keep the old tab wording).

test('Other FieldWorks Lite users view', async ({page}) => {
  await openStory(page, 'sync-sync-dialog--pending');
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', {name: /^Other FieldWorks Lite users/}).click();
  await expect(dialog.getByText('Pending')).toBeVisible();
  await blurFocusedField(page);
  await docsScreenshot(page, 'sync-fw-lite-tab', dialog);
});

test('FieldWorks Send/Receive (on Lexbox) view', async ({page}) => {
  await openStory(page, 'sync-sync-dialog--pending');
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', {name: /^FieldWorks Send\/Receive \(on Lexbox\)/}).click();
  await expect(dialog.getByRole('button', {name: 'Sync', exact: true})).toBeVisible();
  await blurFocusedField(page);
  await docsScreenshot(page, 'sync-lexbox-tab', dialog);
});

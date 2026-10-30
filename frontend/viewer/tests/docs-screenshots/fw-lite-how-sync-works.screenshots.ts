import {expect, test} from '@playwright/test';
import {docsScreenshot, openStory} from './docs-screenshot';

// The Synchronize dialog in the states docs/fw-lite/how-sync-works.md names, captured from the
// sync-primitive stories (the demo project has sync turned off).

const states = [
  // "Up to date": nothing waiting in either direction
  {name: 'sync-up-to-date', story: 'sync-sync-primitive--no-changes'},
  // "Pending": changes waiting, with the Auto syncing button
  {name: 'sync-pending', story: 'sync-sync-primitive--normal'},
  {name: 'sync-offline', story: 'sync-sync-primitive--offline'},
  {name: 'sync-not-logged-in', story: 'sync-sync-primitive--not-logged-in'},
];

for (const {name, story} of states) {
  test(`FieldWorks Lite tab: ${name}`, async ({page}) => {
    const pane = await openStory(page, story);
    await docsScreenshot(page, name, pane);
  });
}

test('Lexbox tab', async ({page}) => {
  const pane = await openStory(page, 'sync-sync-primitive--normal');
  await pane.getByRole('tab', {name: 'Lexbox'}).click();
  await expect(pane.getByRole('button', {name: 'Sync', exact: true})).toBeVisible();
  await docsScreenshot(page, 'sync-lexbox-tab', pane);
});

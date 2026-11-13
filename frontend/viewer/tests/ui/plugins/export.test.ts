import {expect, test} from '@playwright/test';
import {enablePlugins, pluginHtml, previewInStudio, readDownload} from './plugin-helpers';

test.beforeEach(async ({page}) => enablePlugins(page));

test('the Browse Grid example exports its rows as a CSV file', async ({page}) => {
  await page.goto('/testing/project-view/plugins/examples/browse-grid');
  const frame = page.frameLocator('iframe[title="Browse Grid"]');
  await expect(frame.locator('#loading')).toBeHidden({timeout: 20000});

  // The example builds a blob: URL and clicks a download link.
  const downloadPromise = page.waitForEvent('download');
  await frame.getByRole('button', {name: 'Export CSV'}).click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toMatch(/-browse\.csv$/);
  expect((await readDownload(download)).split('\r\n').length).toBeGreaterThan(10);
  await expect(page.getByText(/Exported [\d,.]+ rows to CSV/)).toBeVisible();
});

test('a plugin can export a file through a data: link', async ({page}) => {
  await previewInStudio(page, pluginHtml('Data export', {
    body: [
      '<button id="export">Export</button>',
      '<script>document.getElementById("export").addEventListener("click", () => {',
      '  const a = document.createElement("a");',
      '  a.href = "data:text/plain;charset=utf-8," + encodeURIComponent("exported by a plugin");',
      '  a.download = "data-export.txt";',
      '  document.body.append(a);',
      '  a.click();',
      '});</script>',
    ].join('\n'),
  }));

  const downloadPromise = page.waitForEvent('download');
  await page.frameLocator('iframe[title="Data export"]').getByRole('button', {name: 'Export'}).click();
  const download = await downloadPromise;

  expect(download.suggestedFilename()).toBe('data-export.txt');
  expect(await readDownload(download)).toBe('exported by a plugin');
});

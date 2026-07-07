import {expect, test, type Page} from '@playwright/test';
import {waitForProjectViewReady} from './test-utils';

async function gotoPlugins(page: Page) {
  await page.goto('/testing/project-view');
  await waitForProjectViewReady(page, true);
  await page.getByRole('button', {name: 'Plugins'}).click();
  await expect(page.getByRole('heading', {name: 'Plugins'})).toBeVisible();
}

test('runs the built-in example plugin after consent', async ({page}) => {
  await gotoPlugins(page);

  const exampleCard = page.locator('[data-slot="card"]', {hasText: 'Dictionary stats'});
  await expect(exampleCard).toBeVisible();
  await exampleCard.getByRole('button', {name: 'Run'}).click();

  // First run requires consent and states the sandbox guarantees
  await expect(page.getByText('Run this plugin?')).toBeVisible();
  await expect(page.getByText('No internet access')).toBeVisible();
  await page.getByRole('button', {name: 'Run plugin'}).click();

  // The plugin talks to the project through the sandboxed bridge and renders real data
  const frame = page.frameLocator('iframe[title="Dictionary Stats"]');
  await expect(frame.locator('body')).toContainText(/entr/i, {timeout: 15000});
});

test('creates a plugin from pasted HTML and exchanges data over the bridge', async ({page}) => {
  await gotoPlugins(page);

  await page.getByRole('button', {name: 'New plugin'}).click();
  await page.getByLabel('Name').fill('Bridge test');
  await page.getByLabel('Plugin HTML').fill([
    '<!DOCTYPE html><html><head><title>Bridge test</title></head><body>',
    '<h1 id="msg">Loading…</h1>',
    '<script>fwlite.ready.then(async () => {',
    '  const count = await fwlite.countEntries();',
    '  const ws = await fwlite.getWritingSystems();',
    '  document.getElementById("msg").textContent = `Counted ${count} entries, default vernacular: ${ws.vernacular[0].wsId}`;',
    '});</script>',
    '</body></html>',
  ].join('\n'));
  await page.getByRole('button', {name: 'Add plugin'}).click();

  const card = page.locator('[data-slot="card"]', {hasText: 'Bridge test'});
  await expect(card).toBeVisible();
  await card.getByRole('button', {name: 'Run'}).click();
  await page.getByRole('button', {name: 'Run plugin'}).click();

  const frame = page.frameLocator('iframe[title="Bridge test"]');
  await expect(frame.locator('#msg')).toContainText(/Counted \d+ entries, default vernacular: \w+/, {timeout: 15000});
});

test('plugin writes require user approval and apply after it', async ({page}) => {
  await gotoPlugins(page);

  await page.getByRole('button', {name: 'New plugin'}).click();
  await page.getByLabel('Name').fill('Write test');
  await page.getByLabel('Plugin HTML').fill([
    '<!DOCTYPE html><html><head><title>Write test</title></head><body>',
    '<button id="add">Add word</button><div id="status">idle</div>',
    '<script>fwlite.ready.then(async () => {',
    '  const ws = await fwlite.getWritingSystems();',
    '  document.getElementById("add").addEventListener("click", async () => {',
    '    try {',
    '      const entry = await fwlite.createEntry({lexemeForm: {[ws.vernacular[0].wsId]: "pluginword"}, senses: [{gloss: {[ws.analysis[0].wsId]: "made by plugin"}}]});',
    '      document.getElementById("status").textContent = "created:" + entry.id;',
    '    } catch (error) {',
    '      document.getElementById("status").textContent = "error:" + error.code;',
    '    }',
    '  });',
    '});</script>',
    '</body></html>',
  ].join('\n'));
  await page.getByRole('button', {name: 'Add plugin'}).click();

  const card = page.locator('[data-slot="card"]', {hasText: 'Write test'});
  await card.getByRole('button', {name: 'Run'}).click();
  await page.getByRole('button', {name: 'Run plugin'}).click();

  const frame = page.frameLocator('iframe[title="Write test"]');
  await frame.locator('#add').click();

  // Denying leaves the dictionary untouched and rejects the plugin's promise
  await expect(page.getByText('Plugin wants to add an entry')).toBeVisible();
  await expect(page.getByText('Word (')).toBeVisible();
  await page.getByRole('button', {name: `Don't allow`}).click();
  await expect(frame.locator('#status')).toHaveText('error:permission-denied');

  // Approving applies the write
  await frame.locator('#add').click();
  await expect(page.getByText('Plugin wants to add an entry')).toBeVisible();
  await page.getByRole('button', {name: 'Add entry'}).click();
  await expect(frame.locator('#status')).toContainText('created:');
});

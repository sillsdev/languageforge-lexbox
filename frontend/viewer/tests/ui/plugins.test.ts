import http from 'node:http';
import type {AddressInfo} from 'node:net';
import {expect, test, type Page} from '@playwright/test';
import {examplePlugins} from '../../src/lib/plugins/examples';
import {DemoProjectPage} from './demo-project.page';
import {STORAGE_KEY as RELEASE_CHANNEL_KEY} from '../../src/lib/feature-flags/feature-flags';

// Plugins are a beta-channel feature
test.beforeEach(async ({page}) => {
  await page.addInitScript((key) => localStorage.setItem(key, 'beta'), RELEASE_CHANNEL_KEY);
});

async function gotoPlugins(page: Page) {
  await new DemoProjectPage(page).goto();
  await page.getByRole('button', {name: 'Plugins'}).click();
  await expect(page.getByRole('heading', {name: 'Plugins'})).toBeVisible();
}

test('plugins are hidden outside the beta channel', async ({page}) => {
  await page.addInitScript((key) => localStorage.removeItem(key), RELEASE_CHANNEL_KEY);
  const demo = new DemoProjectPage(page);
  await demo.goto();
  await expect(page.getByRole('button', {name: 'Tasks'})).toBeVisible();
  await expect(page.getByRole('button', {name: 'Plugins'})).toHaveCount(0);
});

test('runs the built-in example plugin after consent', async ({page}) => {
  await gotoPlugins(page);

  const exampleCard = page.locator('[data-slot="card"]', {hasText: 'Dictionary stats'});
  await expect(exampleCard).toBeVisible();
  await exampleCard.getByRole('button', {name: 'Run'}).click();

  // First run requires consent and states the sandbox guarantees
  await expect(page.getByText('Run this plugin?')).toBeVisible();
  await expect(page.getByText('Not allowed to use the internet')).toBeVisible();
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

  // Creating a plugin yourself counts as run consent — no consent card for the author.
  const frame = page.frameLocator('iframe[title="Bridge test"]');
  await expect(frame.locator('#msg')).toContainText(/Counted \d+ entries, default vernacular: \w+/, {timeout: 15000});
});

test('plugin writes require user approval and apply after it', async ({page}) => {
  await gotoPlugins(page);

  await page.getByRole('button', {name: 'New plugin'}).click();
  await page.getByLabel('Name').fill('Write test');
  await page.getByLabel('Plugin HTML').fill([
    '<!DOCTYPE html><html><head><title>Write test</title>',
    '<meta name="fwlite-plugin-permissions" content="edit"></head><body>',
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

  // "Always allow" applies the pending write AND stops asking: the next write shows no dialog
  await frame.locator('#add').click();
  await expect(page.getByText('Plugin wants to add an entry')).toBeVisible();
  await page.getByRole('button', {name: 'Always allow for this plugin'}).click();
  await expect(frame.locator('#status')).toContainText('created:');
  await frame.locator('#add').click();
  await expect(frame.locator('#status')).toContainText('created:');
  await expect(page.getByText('Plugin wants to add an entry')).toBeHidden();
});

test('a plugin without the edit permission cannot even ask to write', async ({page}) => {
  await gotoPlugins(page);

  await page.getByRole('button', {name: 'New plugin'}).click();
  await page.getByLabel('Name').fill('No-edit test');
  await page.getByLabel('Plugin HTML').fill([
    '<!DOCTYPE html><html><head><title>No-edit test</title></head><body>',
    '<div id="status">idle</div>',
    '<script>fwlite.ready.then(async () => {',
    '  try {',
    '    await fwlite.createEntry({lexemeForm: {en: "nope"}});',
    '    document.getElementById("status").textContent = "created";',
    '  } catch (error) {',
    '    document.getElementById("status").textContent = "error:" + error.code;',
    '  }',
    '});</script>',
    '</body></html>',
  ].join('\n'));
  await page.getByRole('button', {name: 'Add plugin'}).click();

  const card = page.locator('[data-slot="card"]', {hasText: 'No-edit test'});
  await expect(card.getByText('Read-only')).toBeVisible();
  await card.getByRole('button', {name: 'Run'}).click();

  // Rejected up front — no dialog ever appears
  const frame = page.frameLocator('iframe[title="No-edit test"]');
  await expect(frame.locator('#status')).toHaveText('error:permission-denied');
  await expect(page.getByText('Plugin wants to add an entry')).toBeHidden();
});

test('a plugin cannot post the app its own window events', async ({page}) => {
  await gotoPlugins(page);

  await page.getByRole('button', {name: 'New plugin'}).click();
  await page.getByLabel('Name').fill('Spoof test');
  await page.getByLabel('Plugin HTML').fill([
    '<!DOCTYPE html><html><head><title>Spoof test</title></head><body>',
    '<div id="status">idle</div>',
    '<script>',
    '  parent.postMessage({type: "notification", message: "spoofed toast"}, "*");',
    '  fwlite.ready.then(async () => {',
    '    document.getElementById("status").textContent = "counted:" + await fwlite.countEntries();',
    '  });',
    '</script>',
    '</body></html>',
  ].join('\n'));
  await page.getByRole('button', {name: 'Add plugin'}).click();

  const card = page.locator('[data-slot="card"]', {hasText: 'Spoof test'});
  await card.getByRole('button', {name: 'Run'}).click();

  const frame = page.frameLocator('iframe[title="Spoof test"]');
  await expect(frame.locator('#status')).toContainText('counted:', {timeout: 15000});
  await expect(page.getByText('spoofed toast')).toHaveCount(0);
});

test('an offline plugin cannot send data out', async ({page}) => {
  const received: string[] = [];
  const server = http.createServer((req, res) => {
    received.push(`${req.method} ${req.url}`);
    res.end();
  });
  server.on('upgrade', (req, socket) => {
    received.push(`upgrade ${req.url}`);
    socket.destroy();
  });
  await new Promise<void>((resolve) => server.listen(0, '127.0.0.1', resolve));
  const {port} = server.address() as AddressInfo;
  const url = `http://127.0.0.1:${port}`;
  try {
    await gotoPlugins(page);

    await page.getByRole('button', {name: 'New plugin'}).click();
    await page.getByLabel('Name').fill('Exfil test');
    await page.getByLabel('Plugin HTML').fill([
      '<!DOCTYPE html><html><head><title>Exfil test</title></head><body>',
      '<div id="status">idle</div><div id="violations"></div>',
      `<form id="form" method="post" action="${url}/form"><input name="secret" value="x"></form>`,
      '<script>',
      '  const violations = new Set();',
      '  document.addEventListener("securitypolicyviolation", (e) => {',
      '    violations.add(e.effectiveDirective);',
      '    document.getElementById("violations").textContent = [...violations].sort().join(" ");',
      '  });',
      '  const attempts = [',
      `    () => fetch("${url}/fetch").catch(() => {}),`,
      `    () => navigator.sendBeacon("${url}/beacon", "secret"),`,
      `    () => { new Image().src = "${url}/image"; },`,
      `    () => new WebSocket("ws://127.0.0.1:${port}/ws"),`,
      '    () => document.getElementById("form").submit(),',
      '  ];',
      '  for (const attempt of attempts) { try { attempt(); } catch {} }',
      '  document.getElementById("status").textContent = "done";',
      '</script>',
      '</body></html>',
    ].join('\n'));
    await page.getByRole('button', {name: 'Add plugin'}).click();

    const card = page.locator('[data-slot="card"]', {hasText: 'Exfil test'});
    await card.getByRole('button', {name: 'Run'}).click();

    const frame = page.frameLocator('iframe[title="Exfil test"]');
    await expect(frame.locator('#status')).toHaveText('done', {timeout: 15000});
    for (const directive of ['connect-src', 'form-action', 'img-src']) {
      await expect(frame.locator('#violations')).toContainText(directive);
    }
    // Give any request that slipped past the CSP time to land before asserting none did.
    await page.waitForTimeout(1000);
    expect(received).toEqual([]);
    // A form submission that got through would have replaced the plugin's document.
    await expect(frame.locator('#status')).toHaveText('done');
  } finally {
    await new Promise((resolve) => server.close(resolve));
  }
});

test('the curated example gallery is offered in full', () => {
  expect(examplePlugins.length).toBeGreaterThanOrEqual(20);
});

test('the example gallery filters across categories by function', async ({page}) => {
  await gotoPlugins(page);
  await page.getByRole('button', {name: 'New plugin'}).click();
  const dialog = page.getByRole('dialog');
  await dialog.getByRole('button', {name: 'Start from an example'}).click();

  // Default browse is grouped into sections.
  await expect(dialog.getByRole('heading', {name: 'Play & learn'})).toBeVisible();

  // A function chip collapses to a flat, cross-cutting result set. In this view each card is
  // prefixed with its primary-function label, so match the name as a substring rather than anchored.
  await dialog.getByRole('button', {name: 'Play & learn', pressed: false}).click();
  await expect(dialog.getByRole('button', {name: 'Crossword'})).toBeVisible();
  // Sentence Sprint's primary function is Enrich, but it also serves Play, so it surfaces here too.
  await expect(dialog.getByRole('button', {name: 'Sentence Sprint'})).toBeVisible();
  // A pure Explore example is excluded.
  await expect(dialog.getByRole('button', {name: 'Browse Grid'})).toBeHidden();

  await dialog.getByRole('button', {name: 'Clear'}).click();
  await expect(dialog.getByRole('button', {name: /^Browse Grid/})).toBeVisible();
});

// One isolated test per example so failures are attributable and the suite parallelises, rather than
// one test serially exercising all of them (which blows a single test timeout).
for (const example of examplePlugins) {
  test(`example plugin “${example.name}” runs against the demo project without erroring`, async ({page}) => {
    const pluginName = `E2E ${example.name}`;
    await gotoPlugins(page);

    await page.getByRole('button', {name: 'New plugin'}).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByLabel('Name').fill(pluginName);
    await dialog.getByRole('button', {name: 'Start from an example'}).click();
    // The card's accessible name is "<name> <description> <badges…>", so anchor on the leading name.
    await dialog.getByRole('button', {name: new RegExp(`^${example.name.replace(/[.*+?^${}()|[\]\\]/g, '\\$&')}`)}).click();
    const addButton = dialog.getByRole('button', {name: 'Add plugin'});
    await expect(addButton).toBeEnabled();
    await addButton.click();

    const card = page.locator('[data-slot="card"]', {hasText: pluginName});
    await expect(card).toBeVisible();
    await card.getByRole('button', {name: 'Run'}).click();

    const frame = page.frameLocator(`iframe[title="${pluginName}"]`);
    // Loading clearing proves the plugin finished talking to the project over the bridge.
    await expect(frame.locator('#loading')).toBeHidden({timeout: 20000});
    await expect(frame.locator('#error')).toBeHidden();
    await expect(frame.getByRole('heading', {name: example.name}).first()).toBeVisible();
  });
}

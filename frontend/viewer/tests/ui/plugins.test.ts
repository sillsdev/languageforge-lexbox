import {expect, test, type Page} from '@playwright/test';
import {examplePlugins} from '../../src/lib/plugins/examples';
import {DemoProjectPage} from './demo-project.page';
import {STORAGE_KEY as RELEASE_CHANNEL_KEY} from '../../src/lib/feature-flags/feature-flags';
import type {UserProjectRole} from '../../src/lib/dotnet-types/generated-types/LcmCrdt/UserProjectRole';

// Plugins are a beta-channel feature
test.beforeEach(async ({page}) => {
  await page.addInitScript((key) => localStorage.setItem(key, 'beta'), RELEASE_CHANNEL_KEY);
});

async function gotoPlugins(page: Page) {
  await new DemoProjectPage(page).goto();
  await page.getByRole('button', {name: 'Plugins'}).click();
  await expect(page.getByRole('heading', {name: 'Plugins', exact: true})).toBeVisible();
}

function pluginHtml(title: string, options: {head?: string; body: string}): string {
  return [
    `<!DOCTYPE html><html><head><title>${title}</title>${options.head ?? ''}</head><body>`,
    options.body,
    '</body></html>',
  ].join('\n');
}

/** Pastes HTML into the open studio; the reply may carry chat prose around the document. */
async function pasteIntoStudio(page: Page, html: string) {
  await page.getByLabel(/Paste (the AI's reply|HTML|a new version)/).fill(`Here is your plugin:\n\n${html}\n\nEnjoy!`);
  await page.getByRole('button', {name: 'Use this version'}).click();
}

/** Builds a plugin in the studio from pasted HTML and adds it, returning to the Plugins page. */
async function addPluginViaStudio(page: Page, html: string) {
  await gotoPlugins(page);
  await page.getByRole('button', {name: 'New plugin'}).click();
  await expect(page.getByTestId('plugin-studio')).toBeVisible();
  await pasteIntoStudio(page, html);
  await page.getByRole('button', {name: 'Add to project'}).click();
  await expect(page.getByText('Added for the team')).toBeVisible();
  await page.getByRole('button', {name: 'Back to plugins'}).click();
}

function projectCard(page: Page, name: string) {
  return page.getByTestId('project-plugin-card').filter({hasText: name});
}

function writeButtonPlugin(title: string) {
  return pluginHtml(title, {
  head: '<meta name="fwlite-plugin-permissions" content="edit">',
  body: [
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
  ].join('\n'),
  });
}

test('plugins are hidden outside the beta channel', async ({page}) => {
  await page.addInitScript((key) => localStorage.removeItem(key), RELEASE_CHANNEL_KEY);
  const demo = new DemoProjectPage(page);
  await demo.goto();
  await expect(page.getByRole('button', {name: 'Tasks'})).toBeVisible();
  await expect(page.getByRole('button', {name: 'Plugins'})).toHaveCount(0);
});

test('runs a project plugin after consent', async ({page}) => {
  await gotoPlugins(page);

  const card = projectCard(page, 'Dictionary Stats');
  await expect(card).toBeVisible();
  await card.getByRole('button', {name: 'Run'}).click();

  // First run requires consent and states the sandbox guarantees
  await expect(page.getByText('Run this plugin?')).toBeVisible();
  await expect(page.getByText('Not allowed to use the internet')).toBeVisible();
  await page.getByRole('button', {name: 'Run plugin'}).click();

  // The plugin talks to the project through the sandboxed bridge and renders real data
  const frame = page.frameLocator('iframe[title="Dictionary Stats"]');
  await expect(frame.locator('body')).toContainText(/entr/i, {timeout: 15000});
});

test('a manager tries an example in one click, and non-managers see no examples', async ({page}) => {
  await gotoPlugins(page);
  const shelf = page.getByRole('region', {name: 'Examples'});
  await expect(shelf).toBeVisible();

  await shelf.getByRole('button', {name: 'Try Gap Finder'}).click();
  await expect(page).toHaveURL(/\/plugins\/examples\/gap-finder$/);
  await expect(page.getByText('Example, not in this project')).toBeVisible();
  // Previewing needs no consent card: the example runs straight away, nothing added yet.
  await expect(page.frameLocator('iframe[title="Gap Finder"]').getByRole('heading', {name: 'Gap Finder'})).toBeVisible({timeout: 15000});
  await expect(page.getByRole('button', {name: 'Add to project'})).toBeEnabled();

  await page.getByTestId('plugin-studio').getByRole('button', {name: 'Close', exact: true}).click();
  await expect(page.getByRole('heading', {name: 'Plugins', exact: true})).toBeVisible();
  await page.evaluate((role) => window.__PLAYWRIGHT_UTILS__.setProjectRole(role), 'Editor' as UserProjectRole);
  await expect(shelf).toBeHidden();
  await expect(page.getByRole('button', {name: 'New plugin'})).toBeHidden();
  await expect(projectCard(page, 'Dictionary Stats').getByRole('button', {name: 'Edit'})).toBeHidden();
  await expect(projectCard(page, 'Dictionary Stats').getByRole('button', {name: 'Run'})).toBeVisible();
});

test('the examples shelf filters across categories by function', async ({page}) => {
  await gotoPlugins(page);
  const shelf = page.getByRole('region', {name: 'Examples'});

  await shelf.getByRole('button', {name: 'Play & learn', pressed: false}).click();
  await expect(shelf.getByRole('button', {name: 'Try Crossword'})).toBeVisible();
  // Sentence Sprint's primary function is Enrich, but it also serves Play, so it shows here too.
  await expect(shelf.getByRole('button', {name: 'Try Sentence Sprint'})).toBeVisible();
  // A pure Explore example is excluded.
  await expect(shelf.getByRole('button', {name: 'Try Browse Grid'})).toBeHidden();

  await shelf.getByRole('button', {name: 'All', pressed: false}).click();
  await shelf.getByRole('button', {name: `See all ${examplePlugins.length} examples`}).click();
  await expect(shelf.getByTestId('plugin-example-card')).toHaveCount(examplePlugins.length);
});

test('creates a plugin from a pasted AI reply and exchanges data over the bridge', async ({page}) => {
  await gotoPlugins(page);
  await page.getByRole('button', {name: 'New plugin'}).click();
  await expect(page.getByText('Your plugin will run here')).toBeVisible();

  await pasteIntoStudio(page, pluginHtml('Bridge test', {
    body: [
      '<h1 id="msg">Loading...</h1>',
      '<script>fwlite.ready.then(async () => {',
      '  const count = await fwlite.countEntries();',
      '  const ws = await fwlite.getWritingSystems();',
      '  document.getElementById("msg").textContent = `Counted ${count} entries, default vernacular: ${ws.vernacular[0].wsId}`;',
      '});</script>',
    ].join('\n'),
  }));

  // It runs in the preview before anyone else can see it, named from its <title>.
  const preview = page.frameLocator('iframe[title="Bridge test"]');
  await expect(preview.locator('#msg')).toContainText(/Counted \d+ entries, default vernacular: \w+/, {timeout: 15000});
  await page.getByRole('tab', {name: 'Details'}).click();
  await expect(page.getByLabel('Name')).toHaveValue('Bridge test');

  await page.getByRole('button', {name: 'Add to project'}).click();
  await expect(page.getByText('Added for the team')).toBeVisible();
  await page.getByRole('button', {name: 'Back to plugins'}).click();
  await projectCard(page, 'Bridge test').getByRole('button', {name: 'Run'}).click();

  // Adding a plugin yourself counts as run consent — no consent card for the author.
  const frame = page.frameLocator('iframe[title="Bridge test"]');
  await expect(frame.locator('#msg')).toContainText(/Counted \d+ entries/, {timeout: 15000});
});

test('a preview write shows what would change and saves nothing', async ({page}) => {
  await gotoPlugins(page);
  const countBefore = await page.evaluate(() => window.__PLAYWRIGHT_UTILS__.demoApi.countEntries());
  await page.getByRole('button', {name: 'New plugin'}).click();
  await pasteIntoStudio(page, writeButtonPlugin('Preview write'));

  const frame = page.frameLocator('iframe[title="Preview write"]');
  await frame.locator('#add').click();

  const dialog = page.getByTestId('plugin-preview-write-dialog');
  await expect(dialog.getByText('Preview: the plugin would add an entry')).toBeVisible();
  await expect(dialog.getByText('Word (')).toBeVisible();
  await expect(dialog.getByRole('button', {name: 'Always allow for this plugin'})).toHaveCount(0);
  await dialog.getByRole('button', {name: 'Keep previewing'}).click();

  await expect(frame.locator('#status')).toHaveText('error:preview');
  expect(await page.evaluate(() => window.__PLAYWRIGHT_UTILS__.demoApi.countEntries())).toBe(countBefore);
});

test('plugin writes require user approval and apply after it', async ({page}) => {
  await addPluginViaStudio(page, writeButtonPlugin('Write test'));
  await projectCard(page, 'Write test').getByRole('button', {name: 'Run'}).click();

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
  await frame.locator('#status').evaluate(element => element.textContent = 'idle');
  await frame.locator('#add').click();
  await expect(frame.locator('#status')).toContainText('created:');
  await expect(page.getByText('Plugin wants to add an entry')).toBeHidden();
});

test('a plugin without the edit permission cannot even ask to write', async ({page}) => {
  await addPluginViaStudio(page, pluginHtml('No-edit test', {
    body: [
      '<div id="status">idle</div>',
      '<script>fwlite.ready.then(async () => {',
      '  try {',
      '    await fwlite.createEntry({lexemeForm: {en: "nope"}});',
      '    document.getElementById("status").textContent = "created";',
      '  } catch (error) {',
      '    document.getElementById("status").textContent = "error:" + error.code;',
      '  }',
      '});</script>',
    ].join('\n'),
  }));

  const card = projectCard(page, 'No-edit test');
  await expect(card.getByText('Read-only')).toBeVisible();
  await card.getByRole('button', {name: 'Run'}).click();

  // Rejected up front — no dialog ever appears
  const frame = page.frameLocator('iframe[title="No-edit test"]');
  await expect(frame.locator('#status')).toHaveText('error:permission-denied');
  await expect(page.getByText('Plugin wants to add an entry')).toBeHidden();
});

test('an example changed before adding is saved as its own "(custom)" plugin', async ({page}) => {
  await gotoPlugins(page);
  await page.getByRole('region', {name: 'Examples'}).getByRole('button', {name: 'Try Rapid Word Collection'}).click();
  await expect(page.getByRole('button', {name: 'Add to project'})).toBeEnabled();

  await page.getByRole('tab', {name: 'Ask AI'}).click();
  await pasteIntoStudio(page, pluginHtml('My rapid words', {body: '<h1>Mine</h1>'}));
  await expect(page.getByText('Your version, not added yet')).toBeVisible();
  await page.getByRole('button', {name: 'Add my version'}).click();
  await expect(page.getByText('Added for the team')).toBeVisible();

  await page.getByRole('button', {name: 'Back to plugins'}).click();
  await expect(projectCard(page, 'Rapid Word Collection (custom)')).toBeVisible();
});

test('editing flags a new permission in the pasted version before it is saved', async ({page}) => {
  await addPluginViaStudio(page, pluginHtml('Offline tool', {body: '<h1>v1</h1>'}));
  await projectCard(page, 'Offline tool').getByRole('button', {name: 'Edit'}).click();
  await expect(page.getByTestId('plugin-studio')).toBeVisible();
  await expect(page.getByRole('button', {name: 'Save for the team'})).toBeDisabled();

  await pasteIntoStudio(page, pluginHtml('Offline tool', {
    head: '<meta name="fwlite-plugin-permissions" content="internet">',
    body: '<h1>v2</h1>',
  }));
  const review = page.getByTestId('plugin-version-review');
  await expect(review.getByText('The new version asks to use the internet')).toBeVisible();
  await expect(page.getByText('Unsaved changes')).toBeVisible();

  // Both versions stay one click apart in the preview.
  const frame = page.frameLocator('iframe[title="Offline tool"]');
  await expect(frame.getByRole('heading')).toHaveText('v2');
  await page.getByRole('button', {name: 'Current version'}).click();
  await expect(frame.getByRole('heading')).toHaveText('v1');
  await page.getByRole('button', {name: 'New version'}).click();
  await expect(frame.getByRole('heading')).toHaveText('v2');

  await review.getByRole('button', {name: 'Keep it'}).click();
  await expect(review.getByText('The new version asks to use the internet')).toBeHidden();
  await page.getByRole('button', {name: 'Save for the team'}).click();
  await expect(page.getByText('Unsaved changes')).toBeHidden();

  await page.getByRole('button', {name: 'Plugins', exact: true}).first().click();
  await expect(projectCard(page, 'Offline tool').getByText('Internet')).toBeVisible();
});

test('leaving the studio with unsaved work asks first', async ({page}) => {
  await gotoPlugins(page);
  await page.getByRole('button', {name: 'New plugin'}).click();
  await pasteIntoStudio(page, pluginHtml('Unsaved', {body: '<h1>draft</h1>'}));

  await page.getByRole('button', {name: 'Tasks'}).click();
  await expect(page.getByText('Leave without saving?')).toBeVisible();
  await page.getByRole('button', {name: 'Keep editing'}).click();
  await expect(page.getByTestId('plugin-studio')).toBeVisible();

  await page.getByRole('button', {name: 'Tasks'}).click();
  await page.getByRole('button', {name: 'Leave', exact: true}).click();
  await expect(page).toHaveURL(/\/tasks$/);
});

test('the curated example gallery is offered in full', () => {
  expect(examplePlugins.length).toBeGreaterThanOrEqual(20);
});

// One isolated test per example so failures are attributable and the suite parallelises. Each runs in
// the studio preview without being added, which also exercises preview mode against every example.
for (const example of examplePlugins) {
  test(`example plugin “${example.name}” runs in the studio preview without erroring`, async ({page}) => {
    await page.goto(`/testing/project-view/plugins/examples/${example.key}`);
    await expect(page.getByTestId('plugin-studio')).toBeVisible();

    const frame = page.frameLocator(`iframe[title="${example.name}"]`);
    // Loading clearing proves the plugin finished talking to the project over the bridge.
    await expect(frame.locator('#loading')).toBeHidden({timeout: 20000});
    await expect(frame.locator('#error')).toBeHidden();
    await expect(frame.getByRole('heading', {name: example.name}).first()).toBeVisible();
  });
}

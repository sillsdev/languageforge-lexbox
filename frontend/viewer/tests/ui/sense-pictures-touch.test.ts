import {expect, test} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

// A valid 96x96 PNG (same one used by sense-pictures.test.ts) so the upload flow yields a real,
// clickable rendered image.
const TEST_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAIAAABt+uBvAAAAjklEQVR42u3QMQ0AAAgDsKlDGJoQiANOriZV0FQPhygQJEiQIEGCBAlCkCBBggQJEiQIQYIECRIkSJAgBAkSJEiQIEGCBCFIkCBBggQJEoQgQYIECRIkSBCCBAkSJEiQIEGCECRIkCBBggQJQpAgQYIECRIkCEGCBAkSJEiQIEEIEiRIkCBBggQhSJCgPwuoEXMcuO2DAAAAAABJRU5ErkJggg==',
  'base64',
);

// Touch-specific: a tap on the corner actions menu can fire a stray click on the image behind it
// once the menu is open. The image must ignore taps while a menu is open so it doesn't also open
// the viewer. (A real device fires that stray click; here we dispatch it directly to reproduce it
// deterministically — Playwright's own tap() is too clean to trigger the ghost click.)
test.use({hasTouch: true});

test('the image ignores a click while the actions menu is open (touch ghost-click)', async ({page}) => {
  await page.setViewportSize({width: 390, height: 844});
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.selectEntryByFilter('ambuka');
  const field = page.locator('[data-field-id="pictures"]').first();
  await field.locator('input[type="file"]').setInputFiles({name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG});
  // Adding a picture now opens the "Add Picture" dialog on a draft; Submit adds it to the sense.
  const addDialog = page.getByRole('dialog');
  await addDialog.getByRole('button', {name: 'Submit'}).click();
  await expect(addDialog).toHaveCount(0);
  await expect(field.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});

  const viewButton = field.getByRole('button', {name: 'View Picture'});
  const viewer = page.getByRole('dialog').filter({has: page.getByRole('heading', {name: 'Picture'})});

  // Positive control: with no menu open, a click on the image opens the viewer (proves the click
  // reaches the handler, so the guarded case below can't pass vacuously).
  await viewButton.dispatchEvent('click');
  await expect(viewer).toBeVisible({timeout: 5000});
  await viewer.getByRole('button', {name: 'Close'}).tap();
  await expect(viewer).toHaveCount(0);

  // Open the actions menu, then fire the stray image click: the viewer must NOT open.
  await field.getByRole('button', {name: 'Picture actions'}).first().tap();
  await expect(page.getByRole('button', {name: 'Edit'})).toBeVisible({timeout: 5000});
  await viewButton.dispatchEvent('click');
  await expect(viewer).toHaveCount(0);
});

// iOS WKWebView can deliver the popstate from history.back() slowly (>100ms). Closing the drawer
// menu pops its history entry just as the Edit dialog pushes one; when the wait for popstate timed
// out, the late popstate was taken as "back" for the dialog: it closed at once and left
// `pointer-events: none` on <body>, freezing the app. Delay popstate to reproduce that timing.
test('Edit from the mobile actions drawer survives a slow popstate', async ({page}) => {
  await page.addInitScript(() => {
    const delayedEvents = new WeakSet<Event>();
    window.addEventListener('popstate', (e) => {
      if (delayedEvents.has(e)) return;
      e.stopImmediatePropagation();
      setTimeout(() => {
        const delayed = new PopStateEvent('popstate', {state: e.state as unknown});
        delayedEvents.add(delayed);
        window.dispatchEvent(delayed);
      }, 300);
    }, {capture: true});
  });
  await page.setViewportSize({width: 390, height: 844});
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.selectEntryByFilter('ambuka');
  const field = page.locator('[data-field-id="pictures"]').first();
  await field.locator('input[type="file"]').setInputFiles({name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG});
  const addDialog = page.getByRole('dialog');
  await addDialog.getByRole('button', {name: 'Submit'}).click();
  await expect(addDialog).toHaveCount(0);
  await expect(field.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});

  await field.getByRole('button', {name: 'Picture actions'}).first().tap();
  await page.getByRole('button', {name: 'Edit'}).tap();
  const editDialog = page.getByRole('dialog').filter({has: page.getByRole('heading', {name: 'Edit Picture'})});
  await expect(editDialog).toBeVisible({timeout: 5000});
  // Give the delayed popstate time to land; the dialog must still be open.
  await page.waitForTimeout(1000);
  await expect(editDialog).toBeVisible();
  // A modal dialog sets `pointer-events: none` on <body> while open; closing it must clear that.
  await editDialog.getByRole('button', {name: 'Cancel'}).tap();
  await expect(editDialog).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => getComputedStyle(document.body).pointerEvents)).not.toBe('none');
});

// Edit from the full-screen viewer's actions drawer closes two back-handled layers at once (the
// drawer, then the viewer), each popping a history entry, while the Edit dialog pushes its own.
// The second popstate must not be taken as "back" for the new Edit dialog.
test('Edit from the full-screen viewer actions drawer opens the editor', async ({page}) => {
  await page.setViewportSize({width: 390, height: 844});
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.selectEntryByFilter('ambuka');
  const field = page.locator('[data-field-id="pictures"]').first();
  await field.locator('input[type="file"]').setInputFiles({name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG});
  const addDialog = page.getByRole('dialog');
  await addDialog.getByRole('button', {name: 'Submit'}).click();
  await expect(addDialog).toHaveCount(0);
  await expect(field.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});

  await field.getByRole('button', {name: 'View Picture'}).tap();
  const viewer = page.getByRole('dialog').filter({has: page.getByRole('heading', {name: 'Picture', exact: true})});
  await expect(viewer).toBeVisible({timeout: 5000});
  await viewer.getByRole('button', {name: 'Picture actions'}).tap();
  await page.getByRole('button', {name: 'Edit'}).tap();

  const editDialog = page.getByRole('dialog').filter({has: page.getByRole('heading', {name: 'Edit Picture'})});
  await expect(editDialog).toBeVisible({timeout: 5000});
  await expect(viewer).toHaveCount(0);
  // Let any trailing popstate land; the editor must still be open.
  await page.waitForTimeout(1000);
  await expect(editDialog).toBeVisible();
  // A modal dialog sets `pointer-events: none` on <body> while open; closing it must clear that.
  await editDialog.getByRole('button', {name: 'Cancel'}).tap();
  await expect(editDialog).toHaveCount(0);
  await expect.poll(() => page.evaluate(() => getComputedStyle(document.body).pointerEvents)).not.toBe('none');
});

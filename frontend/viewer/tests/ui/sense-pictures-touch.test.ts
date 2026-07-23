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
  const field = page.locator('[style*="grid-area: pictures"]').first();
  await field.locator('input[type="file"]').setInputFiles({name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG});
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

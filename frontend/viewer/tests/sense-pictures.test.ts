import {expect, test, type Page} from '@playwright/test';
import {BrowsePage} from './browse-page';

// A tiny valid 1x1 PNG, used to exercise the upload flow without a real image file.
const ONE_PX_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mNk+M8AAAMBAQDJ/pLvAAAAAElFTkSuQmCC',
  'base64',
);

/**
 * Verifies the Sense "Pictures" field:
 * - a sense with pictures renders the image (loaded via getFileStream into a blob url) + caption
 * - a sense without pictures shows the enabled "+ Picture" add button, and uploading a file
 *   through it adds a picture that then renders
 *
 * The demo "nyumba" entry (allWsEntry) has two demo pictures on its sense; other demo
 * entries (e.g. "ambuka") have none.
 */
test.describe('Sense pictures', () => {
  test('displays a picture (and its caption) for a sense that has pictures', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('nyumba');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});

    const image = picturesField.locator('img').first();
    await expect(image).toBeVisible({timeout: 5000});
    // A blob: src proves the full pipeline ran: getFileStream returned a stream that we
    // turned into a Blob and an object url. A broken/missing image would not have this.
    await expect(image).toHaveAttribute('src', /^blob:/);

    // Caption is rendered from the best analysis alternative.
    await expect(picturesField.getByText(/A traditional house|Uma casa tradicional/)).toBeVisible();
  });

  test('shows an enabled "+ Picture" button for a sense with no pictures', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    // "ambuka" has no pictures, so its Pictures field shows the add button.
    await browsePage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});

    const addButton = picturesField.getByRole('button', {name: 'Picture'});
    await expect(addButton).toBeVisible();
    await expect(addButton).toBeEnabled();
    // The add button is the empty state, so no image should be present yet.
    await expect(picturesField.locator('img')).toHaveCount(0);
  });

  test('uploading a picture through the "+ Picture" button adds and renders it', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    await expect(picturesField.locator('img')).toHaveCount(0);

    // Clicking the button opens the OS file picker; drive the hidden input directly instead.
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'photo.png',
      mimeType: 'image/png',
      buffer: ONE_PX_PNG,
    });

    // The uploaded picture is created and re-loaded via getFileStream into a blob url.
    const image = picturesField.locator('img').first();
    await expect(image).toBeVisible({timeout: 5000});
    await expect(image).toHaveAttribute('src', /^blob:/);
  });

  /** Uploads one picture to "ambuka" (which starts empty) and returns the pictures-field locator. */
  async function addOnePicture(page: Page) {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();
    await browsePage.selectEntryByFilter('ambuka');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'photo.png', mimeType: 'image/png', buffer: ONE_PX_PNG,
    });
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});
    return picturesField;
  }

  test('"+ Picture" stays available alongside Replace/Delete once a picture exists', async ({page}) => {
    const picturesField = await addOnePicture(page);

    // The add button is still present even though a picture now exists...
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
    // ...plus the two picture-specific actions.
    await expect(picturesField.getByRole('button', {name: 'Replace Picture'})).toBeVisible();
    await expect(picturesField.getByRole('button', {name: 'Delete Picture'})).toBeVisible();
  });

  test('Delete Picture removes the current picture (after confirmation)', async ({page}) => {
    const picturesField = await addOnePicture(page);

    await picturesField.getByRole('button', {name: 'Delete Picture'}).click();
    // Confirm in the delete dialog (its confirm button is also labelled "Delete Picture").
    await page.getByRole('alertdialog').getByRole('button', {name: 'Delete Picture', exact: true}).click();

    // Picture (and the Replace/Delete actions) are gone; the add button remains.
    await expect(picturesField.locator('img')).toHaveCount(0, {timeout: 5000});
    await expect(picturesField.getByRole('button', {name: 'Delete Picture'})).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Replace Picture'})).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
  });

  test('Replace Picture swaps the current picture in place', async ({page}) => {
    const picturesField = await addOnePicture(page);
    const image = picturesField.locator('img').first();
    const originalSrc = await image.getAttribute('src');

    await picturesField.getByRole('button', {name: 'Replace Picture'}).click();
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'replacement.png', mimeType: 'image/png', buffer: ONE_PX_PNG,
    });

    // Still exactly one picture (replaced, not added), re-loaded into a fresh blob url.
    await expect(picturesField.locator('img')).toHaveCount(1);
    await expect(image).toHaveAttribute('src', /^blob:/);
    await expect(image).not.toHaveAttribute('src', originalSrc ?? '', {timeout: 5000});
  });
});

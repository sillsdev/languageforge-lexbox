import {expect, test, type Page} from '@playwright/test';
import {BrowsePage} from './browse-page';

// A valid 96x96 PNG, used to exercise the upload flow without a real image file. It has real
// dimensions (not 1x1) so the rendered image — and the trash button anchored to its corner —
// occupy a realistic, clickable area.
const TEST_PNG = Buffer.from(
  'iVBORw0KGgoAAAANSUhEUgAAAGAAAABgCAIAAABt+uBvAAAAjklEQVR42u3QMQ0AAAgDsKlDGJoQiANOriZV0FQPhygQJEiQIEGCBAlCkCBBggQJEiQIQYIECRIkSJAgBAkSJEiQIEGCBCFIkCBBggQJEoQgQYIECRIkSBCCBAkSJEiQIEGCECRIkCBBggQJQpAgQYIECRIkCEGCBAkSJEiQIEEIEiRIkCBBggQhSJCgPwuoEXMcuO2DAAAAAABJRU5ErkJggg==',
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
      buffer: TEST_PNG,
    });

    // The uploaded picture is created and re-loaded via getFileStream into a blob url.
    const image = picturesField.locator('img').first();
    await expect(image).toBeVisible({timeout: 5000});
    await expect(image).toHaveAttribute('src', /^blob:/);
  });

  test('re-uploading an existing file adds a second picture that reuses it', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();
    await browsePage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    const fileInput = picturesField.locator('input[type="file"]');

    // First upload creates a picture.
    await fileInput.setInputFiles({name: 'shared.png', mimeType: 'image/png', buffer: TEST_PNG});
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});

    // Uploading the same filename again -> server reports AlreadyExists with the existing
    // mediaUri; that's not an error here, so a second Picture pointing at the same file is added.
    await fileInput.setInputFiles({name: 'shared.png', mimeType: 'image/png', buffer: TEST_PNG});

    // Two pictures now exist (each renders its own image in the flex layout).
    await expect(picturesField.locator('img')).toHaveCount(2, {timeout: 5000});
  });

  /** Uploads one picture to "ambuka" (which starts empty) and returns the pictures-field locator. */
  async function addOnePicture(page: Page) {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();
    await browsePage.selectEntryByFilter('ambuka');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG,
    });
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});
    return picturesField;
  }

  /** Adds a picture, clicks it to open the edit dialog, and returns [picturesField, dialog]. */
  async function openEditor(page: Page) {
    const picturesField = await addOnePicture(page);
    await picturesField.getByRole('button', {name: 'Edit Picture'}).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({timeout: 5000});
    return {picturesField, dialog};
  }

  test('"+ Picture" stays available; clicking a picture opens the edit dialog', async ({page}) => {
    const picturesField = await addOnePicture(page);

    // The add button is still present even though a picture now exists...
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
    // ...and the picture itself is a button that opens the editor (no field-level action buttons).
    const editButton = picturesField.getByRole('button', {name: 'Edit Picture'});
    await expect(editButton).toBeVisible();
    await expect(picturesField.getByRole('button', {name: 'Replace Picture'})).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Delete Picture'})).toHaveCount(0);

    // Opening it reveals the caption editor and the Replace/Delete actions inside the dialog.
    await editButton.click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText('Caption')).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Replace Picture'})).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Delete Picture'})).toBeVisible();

    // Submit dismisses the dialog (leaving the picture in place).
    await dialog.getByRole('button', {name: 'Submit'}).click();
    await expect(dialog).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Edit Picture'})).toBeVisible();
  });

  test('Delete Picture (in the dialog) removes the picture after confirmation', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);

    await dialog.getByRole('button', {name: 'Delete Picture'}).click();
    // Confirm in the delete alert dialog (its confirm button is also labelled "Delete Picture").
    await page.getByRole('alertdialog').getByRole('button', {name: 'Delete Picture', exact: true}).click();

    // Picture is gone and the edit dialog closes; the add button remains.
    await expect(picturesField.locator('img')).toHaveCount(0, {timeout: 5000});
    await expect(page.getByRole('dialog')).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
  });

  test('Replace Picture (in the dialog) swaps the current picture in place', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);
    const fieldImage = picturesField.locator('img').first();
    const originalSrc = await fieldImage.getAttribute('src');

    await dialog.getByRole('button', {name: 'Replace Picture'}).click();
    await dialog.locator('input[type="file"]').setInputFiles({
      name: 'replacement.png', mimeType: 'image/png', buffer: TEST_PNG,
    });

    // Still exactly one picture (replaced, not added), re-loaded into a fresh blob url.
    await expect(picturesField.locator('img')).toHaveCount(1);
    await expect(fieldImage).toHaveAttribute('src', /^blob:/);
    await expect(fieldImage).not.toHaveAttribute('src', originalSrc ?? '', {timeout: 5000});
  });

  test('editing the caption in the dialog shows it under the picture', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);

    // Type into the first writing system's caption editor and commit the change (blur).
    const captionEditor = dialog.locator('[contenteditable="true"]').first();
    await captionEditor.click();
    await captionEditor.pressSequentially('Riverbank');
    await captionEditor.evaluate((el) => (el as HTMLElement).blur());

    // The caption is saved and rendered beneath the picture in the field.
    await expect(picturesField.getByText('Riverbank')).toBeVisible({timeout: 5000});
  });
});

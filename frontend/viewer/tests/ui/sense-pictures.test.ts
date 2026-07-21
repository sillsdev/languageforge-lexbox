import {expect, test, type Locator, type Page} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

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
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    await projectPage.selectEntryByFilter('nyumba');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});

    // The demo's pre-seeded pictures stand in for a remote media service, so they aren't available
    // locally: the field shows a "Click to load" placeholder rather than auto-loading.
    await expect(picturesField.getByRole('button', {name: 'Click to load'}).first()).toBeVisible({timeout: 5000});
    await expect(picturesField.locator('img')).toHaveCount(0);

    // Clicking downloads it — a blob: src proves the full pipeline ran (getFileStream -> Blob -> object url).
    await loadFirstPicture(picturesField);

    // Caption is rendered from the best analysis alternative (shown regardless of image load).
    await expect(picturesField.getByText(/A traditional house|Uma casa tradicional/)).toBeVisible();
  });

  test('shows an enabled "+ Picture" button for a sense with no pictures', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    // "ambuka" has no pictures, so its Pictures field shows the add button.
    await projectPage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});

    const addButton = picturesField.getByRole('button', {name: 'Picture'});
    await expect(addButton).toBeVisible();
    await expect(addButton).toBeEnabled();
    // The add button is the empty state, so no image should be present yet.
    await expect(picturesField.locator('img')).toHaveCount(0);
  });

  test('uploading a picture through the "+ Picture" button adds and renders it', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    await projectPage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    await expect(picturesField.locator('img')).toHaveCount(0);

    // Clicking the button opens the OS file picker; drive the hidden input directly instead.
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'photo.png',
      mimeType: 'image/png',
      buffer: TEST_PNG,
    });

    // An uploaded picture is available locally, so it loads automatically (no "click to load"
    // placeholder) and renders into a blob url.
    const image = picturesField.locator('img').first();
    await expect(image).toBeVisible({timeout: 5000});
    await expect(image).toHaveAttribute('src', /^blob:/);
    await expect(picturesField.getByRole('button', {name: 'Click to load'})).toHaveCount(0);
  });

  test('re-uploading an existing file adds a second picture that reuses it', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await projectPage.selectEntryByFilter('ambuka');

    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    const fileInput = picturesField.locator('input[type="file"]');

    // First upload adds a picture; uploaded files are local, so it loads automatically.
    await fileInput.setInputFiles({name: 'shared.png', mimeType: 'image/png', buffer: TEST_PNG});
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});

    // Uploading the same filename again -> server reports AlreadyExists with the existing mediaUri;
    // it's already cached, so the second picture also renders immediately.
    await fileInput.setInputFiles({name: 'shared.png', mimeType: 'image/png', buffer: TEST_PNG});

    // Both pictures now render an image.
    await expect(picturesField.locator('img')).toHaveCount(2, {timeout: 5000});

    // Both pictures point at the same uploaded file (mediaUri), so the entry-scoped cache backs
    // them with a single shared object URL — the image is loaded once, not once per picture.
    const sources = await picturesField
      .locator('img')
      .evaluateAll((images) => images.map((image) => image.getAttribute('src')));
    expect(sources[0]).toMatch(/^blob:/);
    expect(sources[1]).toBe(sources[0]);
  });

  /** Uploads one picture to "ambuka" (which starts empty) and returns the pictures-field locator. */
  async function addOnePicture(page: Page) {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await projectPage.selectEntryByFilter('ambuka');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField).toBeVisible({timeout: 5000});
    await picturesField.locator('input[type="file"]').setInputFiles({
      name: 'photo.png', mimeType: 'image/png', buffer: TEST_PNG,
    });
    // An uploaded picture is local, so it loads automatically; wait for it before returning.
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});
    return picturesField;
  }

  /** Opens the three-dots actions menu on the first picture in the field. */
  async function openPictureMenu(page: Page, picturesField: Locator) {
    await picturesField.getByRole('button', {name: 'Picture actions'}).first().click();
    // The menu content is portaled to the body (a dropdown at this viewport), so query from the page.
    await expect(page.getByRole('menuitem', {name: 'Edit'})).toBeVisible({timeout: 5000});
  }

  /** Clicks the first "Click to load" placeholder in a scope and waits for its image to load. */
  async function loadFirstPicture(scope: Locator) {
    await scope.getByRole('button', {name: 'Click to load'}).first().click();
    await expect(scope.locator('img').first()).toHaveAttribute('src', /^blob:/, {timeout: 5000});
  }

  /** Adds a picture, opens the edit dialog via the three-dots menu, and returns [picturesField, dialog]. */
  async function openEditor(page: Page) {
    const picturesField = await addOnePicture(page);
    await openPictureMenu(page, picturesField);
    await page.getByRole('menuitem', {name: 'Edit'}).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({timeout: 5000});
    return {picturesField, dialog};
  }

  test('the three-dots menu offers Edit/Download/Delete and Edit opens the editor', async ({page}) => {
    const picturesField = await addOnePicture(page);

    // The add button is still present even though a picture now exists...
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
    // ...and there are no field-level Replace/Delete buttons (those live inside the edit dialog).
    await expect(picturesField.getByRole('button', {name: 'Replace Picture'})).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Delete Picture'})).toHaveCount(0);

    // The corner three-dots menu exposes the three picture actions.
    await openPictureMenu(page, picturesField);
    await expect(page.getByRole('menuitem', {name: 'Edit'})).toBeVisible();
    await expect(page.getByRole('menuitem', {name: 'Download'})).toBeVisible();
    await expect(page.getByRole('menuitem', {name: 'Delete'})).toBeVisible();

    // Edit reveals the caption editor and the Replace/Delete actions inside the dialog.
    await page.getByRole('menuitem', {name: 'Edit'}).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog.getByText('Caption')).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Replace Picture'})).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Delete Picture'})).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Cancel'})).toBeVisible();
    await expect(dialog.getByRole('button', {name: 'Submit'})).toBeVisible();

    // Submit dismisses the dialog (leaving the picture in place).
    await dialog.getByRole('button', {name: 'Submit'}).click();
    await expect(dialog).toHaveCount(0);
    await expect(picturesField.getByRole('button', {name: 'Picture actions'})).toBeVisible();
  });

  test('the three-dots menu Delete removes the picture after confirmation', async ({page}) => {
    const picturesField = await addOnePicture(page);

    await openPictureMenu(page, picturesField);
    await page.getByRole('menuitem', {name: 'Delete'}).click();
    // Confirm in the delete alert dialog (its confirm button is labelled "Delete Picture").
    await page.getByRole('alertdialog').getByRole('button', {name: 'Delete Picture', exact: true}).click();

    await expect(picturesField.locator('img')).toHaveCount(0, {timeout: 5000});
    await expect(picturesField.getByRole('button', {name: 'Picture', exact: true})).toBeVisible();
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

  test('Replace, buffered until Submit, swaps the picture in place', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);
    const fieldImage = picturesField.locator('img').first();
    const originalSrc = await fieldImage.getAttribute('src');

    await dialog.getByRole('button', {name: 'Replace Picture'}).click();
    await dialog.locator('input[type="file"]').setInputFiles({
      name: 'replacement.png', mimeType: 'image/png', buffer: TEST_PNG,
    });

    // The replacement is an uploaded (local) file, so the dialog previews it immediately; the field
    // picture is unchanged until Submit.
    await expect(dialog.locator('img')).toHaveAttribute('src', /^blob:/, {timeout: 5000});
    await expect(fieldImage).toHaveAttribute('src', originalSrc ?? '');

    await dialog.getByRole('button', {name: 'Submit'}).click();

    // Now committed: still one picture (replaced, not added), re-loaded into a fresh blob url.
    await expect(picturesField.locator('img')).toHaveCount(1);
    await expect(fieldImage).toHaveAttribute('src', /^blob:/);
    await expect(fieldImage).not.toHaveAttribute('src', originalSrc ?? '', {timeout: 5000});
  });

  test('editing the caption and pressing Submit shows it under the picture', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);

    // Type into the first writing system's caption editor and commit the field value (blur).
    const captionEditor = dialog.locator('[contenteditable="true"]').first();
    await captionEditor.click();
    await captionEditor.pressSequentially('Riverbank');
    await captionEditor.evaluate((el) => (el as HTMLElement).blur());

    // Buffered: nothing shows under the field picture until Submit.
    await expect(picturesField.getByText('Riverbank')).toHaveCount(0);

    await dialog.getByRole('button', {name: 'Submit'}).click();
    await expect(picturesField.getByText('Riverbank')).toBeVisible({timeout: 5000});
  });

  test('Cancel discards caption edits', async ({page}) => {
    const {picturesField, dialog} = await openEditor(page);

    const captionEditor = dialog.locator('[contenteditable="true"]').first();
    await captionEditor.click();
    await captionEditor.pressSequentially('Discarded');
    await captionEditor.evaluate((el) => (el as HTMLElement).blur());

    await dialog.getByRole('button', {name: 'Cancel'}).click();
    await expect(dialog).toHaveCount(0);

    // The edit never reached the model, so it isn't shown under the picture.
    await expect(picturesField.getByText('Discarded')).toHaveCount(0);
  });

  test('Download Picture (in the dialog) saves the image under its media-server filename', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    // "nyumba" has demo pictures whose media-server filename is deterministic (demo-picture.svg).
    await projectPage.selectEntryByFilter('nyumba');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    // Download works without loading the image; act on the (unloaded) placeholder's actions menu.
    await expect(picturesField.getByRole('button', {name: 'Click to load'}).first()).toBeVisible({timeout: 5000});

    await openPictureMenu(page, picturesField);
    await page.getByRole('menuitem', {name: 'Edit'}).click();
    const dialog = page.getByRole('dialog');
    await expect(dialog).toBeVisible({timeout: 5000});

    const downloadPromise = page.waitForEvent('download');
    await dialog.getByRole('button', {name: 'Download Picture'}).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe('demo-picture.svg');
  });

  test('the three-dots menu Download saves the image under its media-server filename', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await projectPage.selectEntryByFilter('nyumba');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField.getByRole('button', {name: 'Click to load'}).first()).toBeVisible({timeout: 5000});

    const downloadPromise = page.waitForEvent('download');
    await openPictureMenu(page, picturesField);
    await page.getByRole('menuitem', {name: 'Download'}).click();
    const download = await downloadPromise;
    expect(download.suggestedFilename()).toBe('demo-picture.svg');
  });

  test('clicking a picture opens the fullscreen viewer', async ({page}) => {
    const picturesField = await addOnePicture(page);

    await picturesField.getByRole('button', {name: 'View Picture'}).click();
    const viewer = page.getByRole('dialog');
    await expect(viewer).toBeVisible({timeout: 5000});
    await expect(viewer.getByRole('heading', {name: 'Picture', exact: true})).toBeVisible();

    // The picture is shown (loaded into a blob url) and the same three-dots menu is available.
    await expect(viewer.locator('img')).toHaveAttribute('src', /^blob:/, {timeout: 5000});
    await expect(viewer.getByRole('button', {name: 'Picture actions'})).toBeVisible();

    // A freshly-uploaded picture has no caption, and a single picture has no navigation arrows.
    await expect(viewer.getByRole('button', {name: 'Previous picture'})).toHaveCount(0);
    await expect(viewer.getByRole('button', {name: 'Next picture'})).toHaveCount(0);
  });

  test('the fullscreen viewer navigates between pictures and shows their non-empty captions', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    // "nyumba" has two pictures, each with an English and Portuguese caption.
    await projectPage.selectEntryByFilter('nyumba');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    // Load the first picture (click), then click it again to open the viewer.
    await loadFirstPicture(picturesField);

    await picturesField.getByRole('button', {name: 'View Picture'}).first().click();
    const viewer = page.getByRole('dialog');
    await expect(viewer).toBeVisible({timeout: 5000});

    // Both non-empty captions of the first picture are shown.
    await expect(viewer.getByText('A traditional house')).toBeVisible();
    await expect(viewer.getByText('Uma casa tradicional')).toBeVisible();

    // At the first picture, Previous is disabled and Next is enabled.
    const previous = viewer.getByRole('button', {name: 'Previous picture'});
    const next = viewer.getByRole('button', {name: 'Next picture'});
    await expect(previous).toBeDisabled();
    await expect(next).toBeEnabled();

    // Next swaps in the second picture's captions and reaches the end.
    await next.click();
    await expect(viewer.getByText('A modern house')).toBeVisible();
    await expect(viewer.getByText('A traditional house')).toHaveCount(0);
    await expect(next).toBeDisabled();
    await expect(previous).toBeEnabled();

    // Picture 2 wasn't pre-loaded, so the viewer shows its own "click to load"; loading works here too.
    await expect(viewer.getByRole('button', {name: 'Click to load'})).toBeVisible();
    await loadFirstPicture(viewer);

    // Previous returns to the first picture.
    await previous.click();
    await expect(viewer.getByText('A traditional house')).toBeVisible();
    await expect(previous).toBeDisabled();
  });

  test('the fullscreen viewer Edit hands off to the edit dialog', async ({page}) => {
    const picturesField = await addOnePicture(page);

    await picturesField.getByRole('button', {name: 'View Picture'}).click();
    const viewer = page.getByRole('dialog');
    await expect(viewer).toBeVisible({timeout: 5000});

    await viewer.getByRole('button', {name: 'Picture actions'}).click();
    await page.getByRole('menuitem', {name: 'Edit'}).click();

    // The edit dialog takes over (Replace/Submit live only there).
    await expect(page.getByRole('button', {name: 'Replace Picture'})).toBeVisible({timeout: 5000});
    await expect(page.getByRole('button', {name: 'Submit'})).toBeVisible();
  });

  test('the viewer reuses the thumbnail image from the entry-scoped cache (loaded once)', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await projectPage.selectEntryByFilter('nyumba');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    // Load the thumbnail (click), capture its object URL, then open the viewer.
    await loadFirstPicture(picturesField);
    const thumbnailSrc = await picturesField.locator('img').first().getAttribute('src');

    await picturesField.getByRole('button', {name: 'View Picture'}).first().click();
    const viewer = page.getByRole('dialog');
    await expect(viewer).toBeVisible({timeout: 5000});

    // Identical object URL => the cache served the image rather than re-fetching it.
    await expect(viewer.locator('img')).toHaveAttribute('src', thumbnailSrc ?? '', {timeout: 5000});
  });

  test('clicking the viewer captions collapses to the first caption and expands again', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    // "nyumba"'s first picture has two captions (English + Portuguese), i.e. more than one line.
    await projectPage.selectEntryByFilter('nyumba');
    const picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await loadFirstPicture(picturesField);
    await picturesField.getByRole('button', {name: 'View Picture'}).first().click();
    const viewer = page.getByRole('dialog');
    await expect(viewer).toBeVisible({timeout: 5000});

    // Expanded by default: both captions are shown.
    await expect(viewer.getByText('A traditional house')).toBeVisible();
    await expect(viewer.getByText('Uma casa tradicional')).toBeVisible();

    const toggle = viewer.getByRole('button', {name: 'Toggle captions'});
    // A disclosure chevron signals the toggle; it points up (rotated) while expanded.
    const chevron = toggle.locator('.i-mdi-chevron-down');
    await expect(chevron).toBeVisible();
    await expect(chevron).toHaveClass(/rotate-180/);

    // Collapse: only the first non-empty caption remains, and the chevron points down.
    await toggle.click();
    await expect(viewer.getByText('Uma casa tradicional')).toHaveCount(0);
    await expect(viewer.getByText('A traditional house')).toBeVisible();
    await expect(chevron).not.toHaveClass(/rotate-180/);

    // Expand again: both captions return and the chevron points up.
    await toggle.click();
    await expect(viewer.getByText('Uma casa tradicional')).toBeVisible();
    await expect(chevron).toHaveClass(/rotate-180/);
  });

  test('a loaded image stays cached (project-scoped) after navigating to another entry and back', async ({page}) => {
    const projectPage = new DemoProjectPage(page);
    await projectPage.goto();

    // Load the first picture on "nyumba" and remember its object URL.
    await projectPage.selectEntryByFilter('nyumba');
    let picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await loadFirstPicture(picturesField);
    const loadedSrc = await picturesField.locator('img').first().getAttribute('src');

    // Navigate to a different entry, then back to "nyumba".
    await projectPage.selectEntryByFilter('ambuka');
    await projectPage.selectEntryByFilter('nyumba');

    // The cache is project-scoped, so the same object URL is displayed immediately — no re-click,
    // no "click to load" placeholder for that picture.
    picturesField = page.locator('[style*="grid-area: pictures"]').first();
    await expect(picturesField.locator('img').first()).toHaveAttribute('src', loadedSrc ?? '', {timeout: 5000});
  });
});

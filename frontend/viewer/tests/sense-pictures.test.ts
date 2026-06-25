import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

/**
 * Verifies the Sense "Pictures" field:
 * - a sense with pictures renders the image (loaded via getFileStream into a blob url) + caption
 * - a sense without pictures shows the disabled "+ Picture" add button
 *
 * The demo "nyumba" entry's first sense has two demo pictures; its other senses have none.
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

  test('shows a disabled "+ Picture" button for a sense with no pictures', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('nyumba');

    // First sense has pictures; later senses do not, so their Pictures field shows the add button.
    const emptyPicturesField = page.locator('[style*="grid-area: pictures"]').nth(1);
    await expect(emptyPicturesField).toBeVisible({timeout: 5000});

    const addButton = emptyPicturesField.getByRole('button', {name: 'Picture'});
    await expect(addButton).toBeVisible();
    await expect(addButton).toBeDisabled();
  });
});

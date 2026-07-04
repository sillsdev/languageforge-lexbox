import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

const UNSPECIFIED_VARIANT_TYPE_ID = '3942addb-99fd-43e9-ab7d-99025ceb0d4e';
const DIALECTAL_VARIANT_TYPE_ID = '024b62c9-93b3-41a0-ab19-587a0030219a';

type VariantLink = {mainEntryId: string, variantEntryId: string, types: {id: string}[]};

test.describe('Variants', () => {

  test('adding a variant-of link via UI is saved to backend', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const timestamp = Date.now().toString().slice(-6);
    const main = await browsePage.api.createEntryWithHeadword(`mainentry${timestamp}`);
    const variant = await browsePage.api.createEntryWithHeadword(`variantentry${timestamp}`);

    await browsePage.selectEntryByFilter(variant.headword);

    // add the link via the Variant of field's picker
    const variantOfField = page.locator('[style*="grid-area: variantOf"]');
    await variantOfField.getByRole('button', {name: 'Variant of'}).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByPlaceholder(/find (entry|word)/i).fill(main.headword);
    await dialog.getByText(main.headword).first().click();
    await dialog.getByRole('button', {name: /select (entry|word)/i}).click();

    // the link shows in the field and lands in the backend with FLEx's default type
    await expect(variantOfField.getByText(main.headword)).toBeVisible();
    await expect(async () => {
      const entry = await browsePage.api.getEntry(variant.id) as {variantOf: VariantLink[]};
      expect(entry.variantOf).toHaveLength(1);
      expect(entry.variantOf[0].mainEntryId).toBe(main.id);
      expect(entry.variantOf[0].types.map(t => t.id)).toContain(UNSPECIFIED_VARIANT_TYPE_ID);
    }).toPass({timeout: 5000});

    // toggle a variant type through the link's menu
    await variantOfField.getByText(main.headword).click();
    await page.getByRole('menuitem', {name: 'Variant type'}).click();
    await page.getByRole('menuitemcheckbox', {name: 'Dialectal Variant'}).click();
    await page.keyboard.press('Escape');

    await expect(async () => {
      const entry = await browsePage.api.getEntry(variant.id) as {variantOf: VariantLink[]};
      expect(entry.variantOf[0].types.map(t => t.id)).toContain(DIALECTAL_VARIANT_TYPE_ID);
    }).toPass({timeout: 5000});

    // the menu reflects the toggle when reopened (guards the nested-state re-render)
    await variantOfField.getByText(main.headword).click();
    await page.getByRole('menuitem', {name: 'Variant type'}).click();
    await expect(page.getByRole('menuitemcheckbox', {name: 'Dialectal Variant'})).toHaveAttribute('aria-checked', 'true');
    await page.keyboard.press('Escape');
  });

  test('adding a variant via the main entry saves the reverse link', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    const timestamp = Date.now().toString().slice(-6);
    const main = await browsePage.api.createEntryWithHeadword(`mainentryb${timestamp}`);
    const variant = await browsePage.api.createEntryWithHeadword(`variantentryb${timestamp}`);

    await browsePage.selectEntryByFilter(main.headword);

    const variantsField = page.locator('[style*="grid-area: variants"]');
    await variantsField.getByRole('button', {name: 'Variant', exact: true}).click();
    const dialog = page.getByRole('dialog');
    await dialog.getByPlaceholder(/find (entry|word)/i).fill(variant.headword);
    await dialog.getByText(variant.headword).first().click();
    await dialog.getByRole('button', {name: /select (entry|word)/i}).click();

    await expect(variantsField.getByText(variant.headword)).toBeVisible();
    await expect(async () => {
      const entry = await browsePage.api.getEntry(main.id) as {variants: VariantLink[]};
      expect(entry.variants).toHaveLength(1);
      expect(entry.variants[0].variantEntryId).toBe(variant.id);
    }).toPass({timeout: 5000});
  });
});

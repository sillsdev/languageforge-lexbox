import {expect, test} from '@playwright/test';
import {BrowsePage} from './browse-page';

/**
 * Disabled writing systems (unchecked in FLEx's writing-system setup) are hidden from
 * the entry editor, except on fields that actually have data in them.
 * The demo project has a disabled vernacular writing system "st" (Ses) with data
 * only in the lexeme form of "khumbo".
 */
test.describe('Disabled writing systems', () => {

  test('field with data in a disabled writing system shows it', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('khumbo');

    const lexemeFormField = browsePage.entryView.lexemeFormField;
    await expect(lexemeFormField).toBeVisible();
    const sesLabel = lexemeFormField.locator('label', {hasText: 'Ses'});
    await expect(sesLabel).toBeVisible();
    const labelFor = await sesLabel.getAttribute('for');
    expect(labelFor).toBeTruthy();
    await expect(page.locator(`#${labelFor}`)).toHaveValue('ntlo');
  });

  test('clearing the text does not remove the input until another entry is selected', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('khumbo');

    const lexemeFormField = browsePage.entryView.lexemeFormField;
    const sesLabel = lexemeFormField.locator('label', {hasText: 'Ses'});
    await expect(sesLabel).toBeVisible();
    const labelFor = await sesLabel.getAttribute('for');
    const sesInput = page.locator(`#${labelFor}`);

    await sesInput.fill('');
    // sticky: the row must not vanish out from under the user mid-edit
    await expect(sesInput).toBeVisible();
    await expect(sesInput).toHaveValue('');

    // selecting an entry without data in the disabled writing system resets it
    await browsePage.entriesList.clearFilter();
    await browsePage.selectEntryByFilter('nyumba');
    await expect(lexemeFormField.locator('label', {hasText: 'Ses'})).toHaveCount(0);
  });

  test('field without data in a disabled writing system does not show it', async ({page}) => {
    const browsePage = new BrowsePage(page);
    await browsePage.goto();

    await browsePage.selectEntryByFilter('nyumba');

    const lexemeFormField = browsePage.entryView.lexemeFormField;
    await expect(lexemeFormField).toBeVisible();
    // the enabled writing systems are visible
    await expect(lexemeFormField.locator('label', {hasText: 'Sen'}).first()).toBeVisible();
    // but the disabled one with no data on this entry is not
    await expect(lexemeFormField.locator('label', {hasText: 'Ses'})).toHaveCount(0);
  });
});

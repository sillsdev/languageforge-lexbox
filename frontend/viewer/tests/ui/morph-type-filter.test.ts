import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

// The demo API ignores the morph type clause, so this covers the filter UI state, not the entry list.
test('Lite morph type preset follows the view and Reset', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();

  const hint = page.getByText('This view shows only whole words and phrases.');
  const resetButton = page.getByRole('button', {name: 'Reset'});
  function morphTypeTrigger(name: string) {
    return page.getByRole('button', {name: `Morph type ${name}`, exact: true});
  }

  await test.step('Lite starts with the words-and-phrases preset', async () => {
    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await expect(morphTypeTrigger('Words and phrases')).toBeVisible();
    await expect(hint).toBeVisible();
    await expect(resetButton).toBeDisabled();
  });

  await test.step('Show all clears it, Reset brings it back', async () => {
    await page.getByRole('button', {name: 'Show all'}).click();
    await expect(morphTypeTrigger('Any morph type')).toBeVisible();
    await expect(hint).toBeHidden();
    await expect(morphTypeTrigger('Any morph type')).toBeFocused();
    await expect(resetButton).toBeEnabled();

    await resetButton.click();
    await expect(morphTypeTrigger('Words and phrases')).toBeVisible();
    await expect(resetButton).toBeDisabled();
  });

  await test.step('Picking a kind or a group header updates the selection', async () => {
    await page.getByRole('button', {name: 'Show all'}).click();
    await morphTypeTrigger('Any morph type').click();
    await page.getByRole('option', {name: 'stem', exact: true}).click();
    await expect(morphTypeTrigger('Affixes')).toBeVisible();

    await page.getByRole('option', {name: 'Words and phrases'}).click();
    await expect(morphTypeTrigger('Any morph type')).toBeVisible();
    await page.keyboard.press('Escape');
    await expect(page.getByRole('option', {name: 'Words and phrases'})).toBeHidden();
  });

  await test.step('Classic has no morph type filter', async () => {
    await page.keyboard.press('Escape');
    await page.getByRole('button').filter({has: page.locator('.i-mdi-layers')}).click();
    await page.getByRole('radio', {name: 'FieldWorks Classic'}).click();
    await expect(page.getByRole('radio', {name: 'FieldWorks Classic'})).toBeChecked();
    await page.keyboard.press('Escape');

    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await expect(morphTypeTrigger('Any morph type')).toBeVisible();
    await expect(hint).toBeHidden();
  });
});

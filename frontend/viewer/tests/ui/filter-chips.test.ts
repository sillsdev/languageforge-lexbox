import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

test('active filters show as removable chips, capped with a "+N more" chip', async ({page}) => {
  await page.setViewportSize({width: 390, height: 844});
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();

  const filterButton = page.getByRole('button', {name: 'Toggle filters'});
  const chips = page.getByRole('button', {name: /^Remove filter:/});
  const more = page.getByRole('button', {name: /^\+\d+ more$/});
  const clear = page.getByRole('button', {name: 'Clear filters'});

  await test.step('one filter gets one chip', async () => {
    await expect(clear).toHaveCount(0);
    await filterButton.click();
    await page.getByRole('combobox').filter({hasText: 'Any part of speech'}).click();
    await page.getByRole('option', {name: 'Adverb'}).click();
    await page.keyboard.press('Escape');
    await expect(chips).toHaveText(['Part of speech: Adverb']);
    await expect(more).toHaveCount(0);
  });

  await test.step('removing the chip clears its filter', async () => {
    await chips.first().click();
    await expect(chips).toHaveCount(0);
    await expect(clear).toHaveCount(0);
    await expect(filterButton).toBeFocused();
    await filterButton.click();
    await expect(page.getByRole('combobox').filter({hasText: 'Any part of speech'})).toBeVisible();
  });

  await test.step('filters that do not fit in one row collapse into "+N more"', async () => {
    await page.getByRole('combobox').filter({hasText: 'Any part of speech'}).click();
    await page.getByRole('option', {name: 'Adverb'}).click();
    await page.getByPlaceholder('Filter for...').fill('ku');
    await page.getByRole('button', {name: 'Entries missing...'}).click();
    await page.getByRole('option', {name: 'Example sentences'}).click();
    const wsSelect = page.getByText('Any Ws', {exact: true});
    await wsSelect.click();
    await page.getByRole('option').first().click();
    await page.keyboard.press('Escape');
    await expect(wsSelect).toHaveCount(0);
    await page.keyboard.press('Escape');

    // Only one chip fits beside "+2 more" and "Clear filters" at this width
    await expect(chips).toHaveCount(1);
    await expect(more).toHaveText('+2 more');
    const chipBox = await chips.first().boundingBox();
    const clearBox = await clear.boundingBox();
    expect(clearBox!.y).toBeCloseTo(chipBox!.y, 0);

    const popoverContent = page.getByText('Specific field');
    await expect(popoverContent).toBeHidden();
    await more.click();
    await expect(popoverContent).toBeVisible();
    // Reopening the popover keeps the narrowed writing system choice
    await expect(page.getByText('Any Ws', {exact: true})).toHaveCount(0);
    await page.keyboard.press('Escape');
    await expect(popoverContent).toBeHidden();
  });

  await test.step('removing one of several chips keeps focus in the chip row', async () => {
    await chips.first().click();
    await expect(chips).toHaveCount(1);
    await expect(more).toHaveText('+1 more');
    await expect(chips.first()).toBeFocused();
  });

  await test.step('"Clear filters" resets the filters', async () => {
    await clear.click();
    await expect(chips).toHaveCount(0);
    await expect(more).toHaveCount(0);
    await expect(filterButton).toBeFocused();
    await filterButton.click();
    await expect(page.getByRole('combobox').filter({hasText: 'Any part of speech'})).toBeVisible();
    await expect(page.getByPlaceholder('Filter for...')).toHaveValue('');
  });
});

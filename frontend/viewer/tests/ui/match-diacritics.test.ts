import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

test('Match diacritics exactly narrows the search and the empty state can undo it', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.api.createEntryWithHeadword('kabana');
  await projectPage.api.createEntryWithHeadword('kabáná');
  await projectPage.api.createEntryWithHeadword('kabanà');
  await projectPage.api.createEntryWithHeadword('zúzú');

  const {entryRows, searchInput} = projectPage.entriesList;
  const row = (headword: string) => entryRows.filter({hasText: headword});
  const matchSwitch = page.getByRole('switch', {name: 'Match diacritics exactly'});
  const hint = page.getByText('Diacritics must match exactly.');

  await test.step('By default a plain search also finds the marked words', async () => {
    await searchInput.fill('kabana');
    await expect(row('kabana')).toHaveCount(1);
    await expect(row('kabáná')).toHaveCount(1);
    await expect(row('kabanà')).toHaveCount(1);
  });

  await test.step('Turning the switch on hides the marked words', async () => {
    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await expect(matchSwitch).not.toBeChecked();
    await matchSwitch.click();
    await expect(matchSwitch).toBeChecked();
    await expect(row('kabáná')).toHaveCount(0);
    await expect(row('kabanà')).toHaveCount(0);
    await expect(row('kabana')).toHaveCount(1);
    await page.keyboard.press('Escape');
  });

  await test.step('No matches offers to ignore diacritics, unless the query has them', async () => {
    await searchInput.fill('kabanâ');
    await expect(page.getByText('No words found')).toBeVisible();
    await expect(hint).toBeHidden();

    await searchInput.fill('zuzu');
    await expect(hint).toBeVisible();
    await page.getByRole('button', {name: 'Ignore diacritics'}).click();
    await expect(row('zúzú')).toHaveCount(1);
    await expect(hint).toBeHidden();
    await expect(matchSwitch).toHaveCount(0);
    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await expect(matchSwitch).not.toBeChecked();
  });
});

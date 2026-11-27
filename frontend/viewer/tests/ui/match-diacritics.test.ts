import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';

test('Match diacritics exactly narrows the search and the empty state can undo it', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  await projectPage.api.createEntryWithHeadword('kabana');
  await projectPage.api.createEntryWithHeadword('kabáná');

  const {entryRows, searchInput} = projectPage.entriesList;
  const plain = entryRows.filter({hasText: 'kabana'});
  const marked = entryRows.filter({hasText: 'kabáná'});
  const matchSwitch = page.getByRole('switch', {name: 'Match diacritics exactly'});

  await test.step('By default a plain search also finds the marked word', async () => {
    await searchInput.fill('kabana');
    await expect(plain).toHaveCount(1);
    await expect(marked).toHaveCount(1);
  });

  await test.step('Turning the switch on hides the marked word', async () => {
    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await expect(matchSwitch).not.toBeChecked();
    await matchSwitch.click();
    await expect(matchSwitch).toBeChecked();
    await expect(marked).toHaveCount(0);
    await expect(plain).toHaveCount(1);
    await page.keyboard.press('Escape');
  });

  await test.step('No matches offers to ignore diacritics', async () => {
    await searchInput.fill('kabanà');
    await expect(page.getByText('Diacritics must match exactly.')).toBeVisible();
    await searchInput.fill('kabána');
    await page.getByRole('button', {name: 'Ignore diacritics'}).click();
    await expect(page.getByText('Diacritics must match exactly.')).toBeHidden();
    await searchInput.fill('kabana');
    await expect(marked).toHaveCount(1);
  });
});

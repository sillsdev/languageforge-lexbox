import {expect, test} from '@playwright/test';
import {DemoProjectPage} from './demo-project.page';

test('typing in an optional translation field keeps focus there', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();

  await page.getByRole('button', {name: 'Tasks'}).first().click();
  await page.locator('[data-field-key]').filter({hasText: 'Example sentence'}).first().click();
  await page.locator('[data-task-id^="example-sentence-"]').filter({hasText: 'Chichewa'}).click();
  await projectPage.entriesList.entryRows.first().click();

  const popup = page.getByRole('dialog');
  const sentence = popup.getByRole('textbox', {name: 'Chw'});
  const translation = popup.getByRole('textbox', {name: 'Por'});
  await expect(sentence).toBeFocused();

  await translation.click();
  await translation.pressSequentially('ola mundo');

  await expect(translation).toContainText('ola mundo');
  await expect(translation).toBeFocused();
  await expect(sentence).not.toContainText('ola');
});

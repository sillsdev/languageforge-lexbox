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

test('a translation task shows the sentence in the preview and starts in the translation field', async ({page}) => {
  const projectPage = new DemoProjectPage(page);
  await projectPage.goto();
  const {id: entryId} = await projectPage.api.createEntryWithForms({seh: '0000'});
  await page.evaluate(async (entryId) => {
    const api = window.__PLAYWRIGHT_UTILS__.demoApi;
    const senseId = crypto.randomUUID();
    await api.createSense(entryId, {id: senseId, entryId, definition: {}, gloss: {}, semanticDomains: [], exampleSentences: [], pictures: []});
    await api.createExampleSentence(entryId, senseId, {
      id: crypto.randomUUID(), senseId, translations: [], reference: {spans: []},
      sentence: {ny: {spans: [{text: 'Moni', ws: 'ny'}]}},
    });
  }, entryId);

  await page.getByRole('button', {name: 'Tasks'}).first().click();
  await page.locator('[data-field-key="example-sentence:translations"]').click();
  await page.locator('[data-task-id="example-translation-pt"]').click();
  await projectPage.entriesList.entryRows.filter({hasText: '0000'}).click();

  const popup = page.getByRole('dialog');
  await expect(popup.getByText('Moni')).toBeVisible();
  await expect(popup.getByRole('textbox', {name: 'Chw'})).toHaveCount(0);
  await expect(popup.getByRole('textbox', {name: 'Por'})).toBeFocused();
});

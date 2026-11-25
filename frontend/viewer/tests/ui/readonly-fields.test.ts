import {expect, test} from '@playwright/test';
import {SortField} from '$lib/dotnet-types/generated-types/MiniLcm/SortField';

test('Ctrl/Cmd+A in a read-only select selects only its value', async ({page}) => {
  await page.goto('/testing/project-view');
  await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi);
  const {entryId, partOfSpeech} = await page.evaluate(async (order) => {
    const api = window.__PLAYWRIGHT_UTILS__.demoApi;
    const entries = await api.getEntries({offset: 0, count: 500, order});
    const entry = entries.find(e => e.senses[0]?.partOfSpeech)!;
    const pos = entry.senses[0].partOfSpeech!;
    return {entryId: entry.id, partOfSpeech: pos.name['en'] ?? Object.values(pos.name)[0]};
  }, {field: SortField.Headword, writingSystem: 'default', ascending: true} as const);
  await page.goto(`/testing/project-view/browse?entryId=${entryId}&entryOpen=true`);
  await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi);
  await page.evaluate(async () => {
    await window.__PLAYWRIGHT_UTILS__.setWrite(false);
  });

  const field = page.locator('[data-field-id="partOfSpeechId"]:visible [aria-readonly="true"]').first();
  await expect(field).toContainText(partOfSpeech);
  await field.focus();
  await page.keyboard.press('ControlOrMeta+a');
  expect((await page.evaluate(() => getSelection()?.toString()))?.trim()).toBe(partOfSpeech);
});

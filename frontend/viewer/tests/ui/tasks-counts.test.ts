import {expect, test} from '@playwright/test';

import {MorphTypeKind} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphTypeKind';

// Only the count on screen is refreshed when entries change, so the ways to end up with a wrong number are:
// the open task's count not following a save, and the list not catching up afterwards or after a visit elsewhere.
test('task counts follow entry changes in the open task, on the list afterwards and after leaving the view', async ({page}) => {
  await page.goto('/testing/project-view/tasks');
  await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi);

  function toGo(text: string | null): number {
    return Number(/(\d+) (entries|words) to go/.exec(text ?? '')?.[1]);
  }
  // The demo api only emulates this task's filter; the per-language tasks get the entry total.
  const partOfSpeechTask = page.getByRole('button', {name: /^(Part of speech|Grammatical info\.),/});
  const otherTask = page.locator('button[title*="to go"]').first();
  async function partOfSpeechCount() {
    return toGo(await partOfSpeechTask.textContent());
  }
  async function otherCount() {
    return toGo(await otherTask.getAttribute('title'));
  }
  await expect.poll(partOfSpeechCount).toBeGreaterThan(0);
  await expect.poll(otherCount).toBeGreaterThan(0);
  const partOfSpeechBefore = await partOfSpeechCount();
  const otherBefore = await otherCount();

  async function addEntryMissingPartOfSpeech() {
    await page.evaluate(async (morphType) => {
      const id = crypto.randomUUID();
      await window.__PLAYWRIGHT_UTILS__.demoApi.createEntry({
        id,
        lexemeForm: {seh: 'qtaskcount'},
        citationForm: {},
        senses: [{
          id: crypto.randomUUID(),
          entryId: id,
          gloss: {en: 'gloss'},
          definition: {},
          semanticDomains: [],
          exampleSentences: [],
          pictures: [],
        }],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
        homographNumber: 0,
      }, {includeComplexFormsAndComponents: true, autoAddMainPublication: false});
    }, MorphTypeKind.Stem);
  }

  await partOfSpeechTask.click();
  const header = page.locator('h1').locator('..');
  await expect(header).toContainText(`${partOfSpeechBefore} `);

  await addEntryMissingPartOfSpeech();
  await expect(header).toContainText(`${partOfSpeechBefore + 1} `);

  await page.getByRole('button', {name: 'Back to tasks'}).click();
  await expect.poll(partOfSpeechCount).toBe(partOfSpeechBefore + 1);
  await expect.poll(otherCount).toBe(otherBefore + 1);

  // The stats outlive this view, so their subscription to entry changes has to as well.
  await page.getByRole('button', {name: 'Browse'}).click();
  await expect(partOfSpeechTask).toBeHidden();
  await addEntryMissingPartOfSpeech();
  await page.getByRole('button', {name: 'Tasks'}).click();
  await expect.poll(partOfSpeechCount).toBe(partOfSpeechBefore + 2);
  await expect.poll(otherCount).toBe(otherBefore + 2);
});

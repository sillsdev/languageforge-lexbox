import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';
import {MorphTypeKind} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphTypeKind';

// Regression for #1869: an entry must leave (and rejoin) a "Missing ..." filtered list as soon as an edit
// changes whether it matches, without toggling the filter.
test.describe('Missing filter', () => {
  let projectPage: DemoProjectPage;

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
  });

  async function createEntry(headword: string, withSense: boolean): Promise<string> {
    return projectPage.page.evaluate(async ({hw, withSense, morphType}) => {
      const id = crypto.randomUUID();
      await window.__PLAYWRIGHT_UTILS__.demoApi.createEntry({
        id,
        lexemeForm: {seh: hw},
        citationForm: {},
        senses: withSense ? [{
          id: crypto.randomUUID(),
          entryId: id,
          gloss: {en: 'gloss'},
          definition: {},
          semanticDomains: [],
          exampleSentences: [],
          pictures: [],
        }] : [],
        note: {},
        literalMeaning: {},
        morphType,
        components: [],
        complexForms: [],
        complexFormTypes: [],
        publishIn: [],
        homographNumber: 0,
      }, {includeComplexFormsAndComponents: true, autoAddMainPublication: false});
      return id;
    }, {hw: headword, withSense, morphType: MorphTypeKind.Stem});
  }

  async function filterMissing(label: string): Promise<void> {
    const {page} = projectPage;
    await page.getByRole('button', {name: 'Toggle filters'}).click();
    await page.getByRole('button', {name: 'Entries missing...'}).click();
    await page.getByRole('option', {name: label}).click();
    await expect(page.getByRole('button', {name: `Missing: ${label}`})).toBeVisible();
    await page.keyboard.press('Escape');
  }

  function entryRow(headword: string) {
    return projectPage.entriesList.entryRows
      .filter({hasNotText: 'Add to dictionary'})
      .filter({hasText: headword});
  }

  test('entry leaves and rejoins the list as its semantic domains change', async () => {
    const {page} = projectPage;
    const headword = 'qmisssemdom';
    await createEntry(headword, true);
    // The search narrows the list to our entry; only part of the headword, so the
    // "create from search" row can't match the headword.
    await projectPage.entriesList.filterByText('qmisssem');
    await filterMissing('Semantic domain');
    await expect(entryRow(headword)).toHaveCount(1);

    await entryRow(headword).click();
    const semDomField = page.locator('[data-field-id="semanticDomains"]');
    await semDomField.getByRole('combobox').click();
    await page.getByRole('option', {name: 'Fruit'}).click();
    await page.keyboard.press('Escape');
    await expect(semDomField).toContainText('Fruit');

    await expect(entryRow(headword)).toHaveCount(0);

    // and back: removing the domain makes it match again
    await semDomField.getByRole('combobox').click();
    await page.getByRole('option', {name: 'Fruit'}).click();
    await page.keyboard.press('Escape');
    await expect(semDomField).not.toContainText('Fruit');

    await expect(entryRow(headword)).toHaveCount(1);
  });

  test('entry leaves the list when it gets a sense', async () => {
    const headword = 'qmisssense';
    const entryId = await createEntry(headword, false);
    await projectPage.entriesList.filterByText('qmisssen');
    await filterMissing('Senses');
    await expect(entryRow(headword)).toHaveCount(1);

    await entryRow(headword).click();
    await projectPage.entryView.addSense();
    await projectPage.entryView.editGloss('new gloss');
    await expect.poll(() => projectPage.api.getEntrySenseCount(entryId)).toBe(1);

    await expect(entryRow(headword)).toHaveCount(0);
  });
});

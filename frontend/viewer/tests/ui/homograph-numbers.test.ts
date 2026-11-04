import {expect, test} from '@playwright/test';

import {DemoProjectPage} from './demo-project.page';
import {MorphTypeKind} from '$lib/dotnet-types/generated-types/MiniLcm/Models/MorphTypeKind';

// FLEx shows homograph number N > 0 as a subscript after the headword (bank₁, bank₂); 0 shows nothing.
test.describe('Homograph numbers', () => {
  let projectPage: DemoProjectPage;
  const headword = 'qhomograph';

  test.beforeEach(async ({page}) => {
    projectPage = new DemoProjectPage(page);
    await projectPage.goto();
    await page.evaluate(async ({hw, morphType}) => {
      const api = window.__PLAYWRIGHT_UTILS__.demoApi;
      for (const homographNumber of [1, 2, 0]) {
        await api.createEntry({
          id: crypto.randomUUID(),
          // the 0 entry gets a different headword, so it can be told apart
          lexemeForm: {seh: homographNumber ? hw : hw + 'solo'},
          citationForm: {},
          senses: [],
          note: {},
          literalMeaning: {},
          morphType,
          components: [],
          complexForms: [],
          complexFormTypes: [],
          publishIn: [],
          homographNumber,
        }, {includeComplexFormsAndComponents: true, autoAddMainPublication: false});
      }
    }, {hw: headword, morphType: MorphTypeKind.Stem});
    // Only part of the headword, so the "create from search" row can't match it.
    await projectPage.entriesList.filterByText('qhomogr');
  });

  function entryRows() {
    return projectPage.entriesList.entryRows.filter({hasNotText: 'Add to dictionary'});
  }

  function rowWithHomographNumber(n: string) {
    return entryRows().filter({has: projectPage.page.getByTestId('homograph-number').filter({hasText: new RegExp(`^${n}$`)})});
  }

  test('entry list shows the homograph number as a subscript, and nothing for 0', async () => {
    const rows = entryRows();
    await expect(rows).toHaveCount(3);
    await expect(rows.filter({hasText: 'qhomographsolo'}).getByTestId('homograph-number')).toHaveCount(0);
    await expect(rowWithHomographNumber('1')).toHaveCount(1);
    await expect(rowWithHomographNumber('2')).toHaveCount(1);
  });

  test('entry editor title and dictionary preview show the homograph number', async ({page}) => {
    await rowWithHomographNumber('2').click();
    await projectPage.entryView.waitForEntryLoaded();

    // The plain-text title uses Unicode subscript digits; the visible heading uses <sub>
    const title = page.locator(`h2[title="${headword}₂"]`);
    await expect(title).toBeVisible();
    await expect(title.getByTestId('homograph-number')).toHaveText('2');

    const preview = page.locator('strong', {hasText: headword});
    await expect(preview.getByTestId('homograph-number')).toHaveText('2');
  });
});

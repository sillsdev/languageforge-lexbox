import {expect, test} from '@playwright/test';

const tasksUrl = '/testing/project-view/tasks';

test('tasks page: open, close, deep-link back, help card', async ({page}) => {
  await page.goto(tasksUrl);
  await expect(page.getByRole('heading', {name: 'All tasks'})).toBeVisible();

  let taskId = '';
  let otherTaskId = '';
  await test.step('picking a multi-language field shows its languages; starting a task opens it', async () => {
    // Rows that start a task directly carry data-task-id; the others have a language step.
    const field = page.locator('[data-field-key]:not([data-task-id])').first();
    await field.click();
    await expect(page).toHaveURL(/[?&]field=/);
    await expect(page.locator('#task-languages-heading')).toBeVisible();
    const languageRow = page.locator('[data-task-id]:not([data-field-key])').first();
    await expect(languageRow).toBeVisible();
    taskId = (await languageRow.getAttribute('data-task-id'))!;
    otherTaskId = (await page.locator('[data-task-id]:not([data-field-key])').nth(1).getAttribute('data-task-id'))!;
    await languageRow.click();
    await expect(page).toHaveURL(new RegExp(`[?&]taskId=${taskId}`));
    await expect(page.getByRole('button', {name: 'Back to tasks'}).first()).toBeVisible();
  });

  await test.step('Back button returns to the list, focuses the task row, and lists it under Recent', async () => {
    await page.getByRole('button', {name: 'Back to tasks'}).first().click();
    await expect(page).not.toHaveURL(/taskId=/);
    await expect(page.getByRole('heading', {name: 'All tasks'})).toBeVisible();
    await expect(page.locator(`[data-task-id="${taskId}"]`)).toBeFocused();
    await expect(page.getByRole('heading', {name: 'Recent'})).toBeVisible();
    await expect(page.locator(`[data-recent-task-id="${taskId}"]`)).toBeVisible();
  });

  await test.step('a deep-linked task counts as recent, and browser Back after closing it does not reopen it', async () => {
    await page.goto(`${tasksUrl}?taskId=${otherTaskId}`);
    await expect(page.getByRole('button', {name: 'Back to tasks'}).first()).toBeVisible();
    await page.getByRole('button', {name: 'Back to tasks'}).first().click();
    await expect(page.getByRole('heading', {name: 'All tasks'})).toBeVisible();
    await expect(page.locator(`[data-recent-task-id="${otherTaskId}"]`)).toBeVisible();
    await page.goBack();
    await expect(page).not.toHaveURL(/taskId=/);
    await expect(page.getByRole('button', {name: 'Back to tasks'})).toHaveCount(0);
  });

  await test.step('help card stays dismissed after reload', async () => {
    // Opening a task above already dismissed it; reopen it to test dismissing explicitly.
    await page.goto(tasksUrl);
    const help = page.getByRole('button', {name: 'How tasks work'});
    await help.click();
    await expect(page.locator('#tasks-help')).toBeVisible();
    await page.getByRole('button', {name: 'Got it'}).click();
    await expect(page.locator('#tasks-help')).toBeHidden();
    await page.reload();
    await expect(page.getByRole('heading', {name: 'All tasks'})).toBeVisible();
    await expect(page.locator('#tasks-help')).toBeHidden();
  });
});

test('tasks page: Back skips field picks on desktop but steps out of a field on phones', async ({page}) => {
  const multiLanguageField = page.locator('[data-field-key]:not([data-task-id])');

  await test.step('desktop: picking fields replaces history', async () => {
    await page.goto('/testing/project-view/browse');
    await page.goto(tasksUrl);
    await multiLanguageField.nth(0).click();
    await multiLanguageField.nth(1).click();
    await expect(page).toHaveURL(/[?&]field=/);
    await page.goBack();
    await expect(page).toHaveURL(/\/browse/);
  });

  await test.step('phone: Back leaves the picked field', async () => {
    await page.setViewportSize({width: 375, height: 812});
    await page.goto(tasksUrl);
    await multiLanguageField.first().click();
    await expect(page.locator('#task-languages-heading')).toBeVisible();
    await page.goBack();
    await expect(page).not.toHaveURL(/field=/);
    await expect(page.getByRole('heading', {name: 'All tasks'})).toBeVisible();
  });
});

type DoubleSaveWindow = {senseSaves: number, holdIndexLookups?: Promise<void>, releaseIndexLookups?: () => void};

test('task drawer: Next again while moving to the next entry does not save the subject twice', async ({page}) => {
  await page.goto(`${tasksUrl}?taskId=missing-part-of-speech`);
  await page.waitForFunction(() => window.__PLAYWRIGHT_UTILS__?.demoApi);
  await page.evaluate(async () => {
    const api = window.__PLAYWRIGHT_UTILS__.demoApi;
    const w = window as unknown as DoubleSaveWindow;
    w.senseSaves = 0;
    const updateSense = api.updateSense.bind(api);
    api.updateSense = async (...args) => {
      w.senseSaves++;
      const result = await updateSense(...args);
      // Like a slow real save: the list drops the saved entry first, so finding the next one asks the api.
      while ([...document.querySelectorAll('[role=row]')].some(row => row.textContent?.includes('doublesave'))) {
        await new Promise(resolve => setTimeout(resolve, 50));
      }
      return result;
    };
    const getEntryIndex = api.getEntryIndex.bind(api);
    api.getEntryIndex = async (...args) => {
      await w.holdIndexLookups;
      return getEntryIndex(...args);
    };
    const id = crypto.randomUUID();
    await api.createEntry({
      id, lexemeForm: {seh: '!!doublesave'}, citationForm: {}, note: {}, literalMeaning: {},
      senses: [{id: crypto.randomUUID(), entryId: id, gloss: {en: 'gloss'}, definition: {}, semanticDomains: [], exampleSentences: [], pictures: []}],
      components: [], complexForms: [], complexFormTypes: [], publishIn: [],
    } as unknown as Parameters<typeof api.createEntry>[0], {includeComplexFormsAndComponents: true, autoAddMainPublication: false});
  });

  await page.getByRole('row', {name: /doublesave/}).click();
  const dialog = page.getByRole('dialog');
  const progress = dialog.getByRole('progressbar', {name: 'Progress'});
  await expect(progress).toHaveAttribute('aria-valuetext', '0 of 10 done');
  await dialog.getByRole('combobox').first().click();
  await page.getByRole('option').first().click();
  await page.evaluate(() => {
    const w = window as unknown as DoubleSaveWindow;
    w.holdIndexLookups = new Promise(resolve => w.releaseIndexLookups = resolve);
  });
  const next = dialog.getByRole('button', {name: 'Next'});
  await next.click();
  await expect(progress).toHaveAttribute('aria-valuetext', '1 of 10 done');
  await next.click();
  await page.evaluate(() => (window as unknown as DoubleSaveWindow).releaseIndexLookups!());

  await expect(dialog.getByText('!!doublesave')).toBeHidden();
  expect(await page.evaluate(() => (window as unknown as DoubleSaveWindow).senseSaves)).toBe(1);
});

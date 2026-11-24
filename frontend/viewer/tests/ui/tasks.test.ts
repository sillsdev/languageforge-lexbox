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

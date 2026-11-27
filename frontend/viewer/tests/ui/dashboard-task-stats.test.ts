import {expect, test, type Locator} from '@playwright/test';

async function numbers(locator: Locator): Promise<number[]> {
  return ((await locator.innerText()).match(/[\d,.]+/g) ?? []).map(n => parseInt(n.replace(/\D/g, ''), 10));
}

// The demo API only emulates the part of speech task's filter, so that's the one whose counts can be compared.
test('dashboard part of speech stat agrees with the task it links to; translation rows link to theirs', async ({page}) => {
  await page.goto('/testing/project-view/dashboard');
  const posCard = page.getByRole('link', {name: /Entries with (a part of speech|grammatical info\.)/});
  await expect(posCard).toContainText(/%/);
  const [, posFilled, posTotal] = await numbers(posCard);
  const translationRow = page.getByRole('link', {name: /^Example translations/}).first();
  await expect(translationRow).toContainText('/');
  const translationTaskId = new URL((await translationRow.getAttribute('href'))!, page.url()).searchParams.get('taskId');
  expect(translationTaskId).toMatch(/^example-translation-/);

  await posCard.click();
  await expect(page).toHaveURL(/taskId=missing-part-of-speech/);

  await page.getByRole('button', {name: 'Back to tasks'}).click();
  const posTask = page.locator('[data-task-id="missing-part-of-speech"]');
  await expect(posTask).toContainText(/\d+ (entries|words) to go/);
  expect((await numbers(posTask.getByText(/to go/)))[0]).toBe(posTotal - posFilled);
});

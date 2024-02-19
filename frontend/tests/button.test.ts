import { test } from './fixtures';

test('Playwright doesn\'t click on loading buttons', async ({ page }) => {
  await page.goto('/sandbox');
  await page.click('text=Primary Button');
  await page.getByText('Loading Button', { exact: true }).waitFor();
  test.fail(true); // everything prior to this should succeed
  await page.getByText('Loading Button', { exact: true }).click({ timeout: 3000 });
});

import { expect, test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';

test('can navigate to project page', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  const adminPage = await new AdminDashboardPage(page).waitFor();
  await adminPage.openProject('Sena 3', 'sena-3');
});

// Regression test for #2224: keystrokes arriving while the debounced URL write was
// round-tripping got clobbered. A ~25ms per-keystroke delay reliably reproduces.
test.describe('user filter typing', () => {
  test.use({ ignoreHTTPSErrors: true });

  test('does not lose characters while typing rapidly', async ({ page }) => {
    await LoginPage.loginAsAdmin(page);
    const adminPage = await new AdminDashboardPage(page).waitFor();

    const input = adminPage.userFilterBarInput;
    await input.click();

    const text = 'abcdefghij';
    const PER_KEYSTROKE_DELAY_MS = 25;
    await input.pressSequentially(text, { delay: PER_KEYSTROKE_DELAY_MS });

    await expect(input, 'input must contain every character typed').toHaveValue(text);
  });
});

// Covers the other half of the #2224 fix: external writes to the URL-backed filter store
// (e.g. onUserCreated, filterProjectsByUser) must still sync into the input.
test.describe('user filter external sync', () => {
  test.use({ ignoreHTTPSErrors: true });

  test('shows userSearch from URL on initial mount', async ({ page }) => {
    await LoginPage.loginAsAdmin(page);
    await page.goto('/admin?userSearch=external');
    const adminPage = await new AdminDashboardPage(page).waitFor();
    await expect(adminPage.userFilterBarInput).toHaveValue('external');
  });

  test('syncs into input when store is written after mount', async ({ page }) => {
    await LoginPage.loginAsAdmin(page);
    const adminPage = await new AdminDashboardPage(page).waitFor();

    await adminPage.userFilterBarInput.fill('typed');
    // Let the debounced write settle so the next store change isn't mistaken for our own echo.
    await page.waitForURL((url) => url.searchParams.get('userSearch') === 'typed');

    // pushState+popstate mimics in-component URL writes (onUserCreated, filterProjectsByUser)
    // that mutate the sveltekit-search-params store without remounting FilterBar.
    await page.evaluate(() => {
      history.pushState({}, '', '/admin?userSearch=external');
      dispatchEvent(new PopStateEvent('popstate'));
    });

    await expect(adminPage.userFilterBarInput).toHaveValue('external');
  });
});

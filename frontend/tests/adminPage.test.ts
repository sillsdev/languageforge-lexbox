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

// Time to wait past FilterBar's 200ms round-trip ignore window so external store writes
// are no longer treated as our own debounced echo. See FilterBar.svelte (#2224).
const ROUND_TRIP_SETTLE_MS = 500;

// Regression test for #2224. The original bug: keystrokes arriving while the debounced write
// is round-tripping through the URL store got clobbered. A per-keystroke delay around the
// debounce window (~25–50ms with CDP overhead) reliably reproduces; verified by reverting
// FilterBar.svelte to the pre-fix shape and watching this fail 5/5.
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
// (e.g. onUserCreated, filterProjectsByUser) must sync into the input. Without the
// time-gated $effect in FilterBar.svelte, the input stays stuck on whatever was last typed.
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
    // Let the debounced write round-trip past the ignore window so the next change is
    // treated as external, not as our own echo.
    await page.waitForTimeout(ROUND_TRIP_SETTLE_MS);

    // Simulate the in-component URL writes done by onUserCreated / filterProjectsByUser,
    // which mutate the sveltekit-search-params store without remounting FilterBar.
    await page.evaluate(() => {
      history.pushState({}, '', '/admin?userSearch=external');
      dispatchEvent(new PopStateEvent('popstate'));
    });

    await expect(adminPage.userFilterBarInput).toHaveValue('external');
  });
});

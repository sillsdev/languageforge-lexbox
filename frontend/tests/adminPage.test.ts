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

// Regression test for #2224: characters typed in quick succession used to get dropped when a
// URL round-trip reset the input mid-typing. Asserts the invariant — every typed character
// survives — rather than a specific internal mechanism; the per-keystroke delay paces the typing.
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

// Covers the other half of the #2224 fix: external changes to the URL-backed filter store
// (deep links, browser back/forward) must still sync into the input.
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

    // Simulate an external URL change (deep link / browser back-forward): pushState changes
    // location.href and SvelteKit's popstate handler propagates it to the $app/state `page`,
    // which runed's useSearchParams observes and syncs into the input. NB this is a different
    // path from the in-component writes (onUserCreated, filterProjectsByUser), which mutate the
    // runed proxy directly — this covers the URL-driven side, not those handlers themselves.
    await page.evaluate(() => {
      history.pushState({}, '', '/admin?userSearch=external');
      dispatchEvent(new PopStateEvent('popstate'));
    });

    await expect(adminPage.userFilterBarInput).toHaveValue('external');
  });

  // Targets the project filter (no `debounce` prop). Before the onClearFiltersClick fix,
  // clicking ✕ wrote `undefined` to the URL-backed store, which normalized to '' and
  // trapped pendingEcho — silently blocking any later external write to the same key.
  test('external write after clearing the filter still reaches the input', async ({ page }) => {
    await LoginPage.loginAsAdmin(page);
    const adminPage = await new AdminDashboardPage(page).waitFor();

    await adminPage.projectFilterBarInput.fill('typed');
    await page.waitForURL((url) => url.searchParams.get('projectSearch') === 'typed');

    const projectFilterBar = page.locator('.filter-bar').nth(0);
    await projectFilterBar.getByRole('button', { name: '✕' }).click();
    await expect(adminPage.projectFilterBarInput).toHaveValue('');
    // Wait for the X-click's goto to finish too — otherwise pushState below races
    // with the in-flight navigation removing projectSearch from the URL.
    await page.waitForURL((url) => !url.searchParams.has('projectSearch'));

    await page.evaluate(() => {
      history.pushState({}, '', '/admin?projectSearch=external');
      dispatchEvent(new PopStateEvent('popstate'));
    });

    await expect(adminPage.projectFilterBarInput).toHaveValue('external');
  });
});

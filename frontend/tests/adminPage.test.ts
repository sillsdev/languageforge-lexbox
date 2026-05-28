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

// Regression test for #2224. The original bug: keystrokes arriving while the debounced write
// is round-tripping through the URL store got clobbered. A per-keystroke delay around the
// debounce window (~25–50ms with CDP overhead) reliably reproduces; verified by reverting
// FilterBar.svelte to the pre-fix shape and watching this fail 5/5.
test.describe('user filter typing', () => {
  test.use({ ignoreHTTPSErrors: true });

  test('does not lose characters while typing rapidly', async ({ page }) => {
    const loginPage = await new LoginPage(page).goto();
    await loginPage.fillForm('admin', defaultPassword);
    await loginPage.submit();
    const adminPage = await new AdminDashboardPage(page).waitFor();

    const input = adminPage.userFilterBarInput;
    await input.click();

    const text = 'abcdefghij';
    await input.pressSequentially(text, { delay: 25 });

    // Wait for any debounce + URL round-trip to settle.
    await page.waitForTimeout(800);

    const actual = await input.inputValue();
    expect.soft(actual, 'input must not be empty after typing').not.toBe('');
    expect(actual, 'input must contain every character typed').toBe(text);
  });
});

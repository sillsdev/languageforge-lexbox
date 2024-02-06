import { expect, test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';

test('can log in', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  await new AdminDashboardPage(page).waitFor();
});

test('show error withour username', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('', defaultPassword);
  await loginPage.submit();
  await loginPage.waitFor();

  await expect(page.getByText('User info missing')).toBeVisible();
});

test('can log in after error', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  expect(defaultPassword).not.toBe('bad');

  // Bad password is bad
  await loginPage.fillForm('admin', 'bad');
  await loginPage.submit();
  await loginPage.waitFor();

  await expect(page.getByText('The account information you entered is incorrect')).toBeVisible();

  // Correct password still works
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  await new AdminDashboardPage(page).waitFor();
});

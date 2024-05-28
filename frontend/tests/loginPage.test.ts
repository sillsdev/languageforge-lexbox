import { expect, test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';
import {ProjectPage} from './pages/projectPage';

test('can log in', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  await new AdminDashboardPage(page).waitFor();
});

test('show error without username', async ({ page }) => {
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

test('after login user is sent to original page', async ({ page }) => {
  const projectPage = await new ProjectPage(page, 'Sena 3', 'sena-3').goto({expectRedirect: true});
  const loginPage = await new LoginPage(page).waitFor();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();//should redirect user to project page
  await projectPage.waitFor();
});

import { test } from '@playwright/test';
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

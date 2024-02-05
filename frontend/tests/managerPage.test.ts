import { expect, test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { UserDashboardPage } from './pages/userDashboardPage';

test('display dashboard, then project page', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto();
  await loginPage.fillForm('manager', defaultPassword);
  await loginPage.submit();
  const userDashboardPage = await new UserDashboardPage(page).waitFor();
  await userDashboardPage.openProject('Sena 3', 'sena-3');
  await expect(page.getByText('Project Code: sena-3')).toBeVisible();
});

import { test } from '@playwright/test';
import { LoginPage } from './pages/loginPage';
import { defaultPassword } from './envVars';
import { AdminDashboardPage } from './pages/adminDashboardPage';

test('can navigate to project page', async ({ page }) => {
  const loginPage = await new LoginPage(page).goto<LoginPage>();
  await loginPage.fillForm('admin', defaultPassword);
  await loginPage.submit();
  // TODO: Port admin page and finish this test
  const adminPage = await new AdminDashboardPage(page).waitFor();
  await adminPage.openProject('Sena 3', 'sena-3');
});

/*
using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class AdminPageTest : PageTest
{
    [Fact]
    public async Task CanNavigateToProjectPage()
    {
        var loginPage = await new LoginPage(Page).Goto();
        await loginPage.FillForm("admin", TestingEnvironmentVariables.DefaultPassword);

        await loginPage.Submit();
        var adminPage = await new AdminDashboardPage(Page).WaitFor();

        await adminPage.OpenProject("Sena 3", "sena-3");
    }
}
*/

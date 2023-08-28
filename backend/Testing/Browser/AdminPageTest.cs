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

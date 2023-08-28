using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class ManagerPageTest : PageTest
{
    [Fact]
    public async Task DisplayDashboardThenProjectPage()
    {
        var loginPage = await new LoginPage(Page).Goto();
        await loginPage.FillForm("manager", TestingEnvironmentVariables.DefaultPassword);

        await loginPage.Submit();
        var userDashboardPage = await new UserDashboardPage(Page).WaitFor();

        await userDashboardPage.OpenProject("sena 3", "sena-3");
        await Expect(Page.GetByText("Project Code: sena-3")).ToBeVisibleAsync();
    }
}

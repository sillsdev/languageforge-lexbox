using Testing.Browser.Base;
using Testing.Browser.Page;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class LoginPageTests : PageTest
{
    [Fact]
    public async Task CanLogin()
    {
        var loginPage = await new LoginPage(Page).Goto();
        await loginPage.FillForm("admin", TestingEnvironmentVariables.DefaultPassword);
        await loginPage.Submit();
        await new AdminDashboardPage(Page).WaitFor();
    }

    [Fact]
    public async Task ShowErrorWithoutUsername()
    {
        var loginPage = await new LoginPage(Page).Goto();
        await loginPage.FillForm("", "pass");
        await loginPage.Submit();
        await loginPage.WaitFor();

        await Expect(Page.GetByText("User info missing")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanLoginAfterError()
    {
        var loginPage = await new LoginPage(Page).Goto();
        await loginPage.FillForm("admin", "bad");
        await loginPage.Submit();
        await loginPage.WaitFor();

        await Expect(Page.GetByText("Something went wrong, please make sure you have used the correct account informa")).ToBeVisibleAsync();

        await loginPage.FillForm("admin", TestingEnvironmentVariables.DefaultPassword);
        await loginPage.Submit();
        await new AdminDashboardPage(Page).WaitFor();
    }
}

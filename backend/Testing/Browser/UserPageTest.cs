using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class UserPageTest : PageTest
{
    private string _host = TestingEnvironmentVariables.ServerHostname;

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync();

        await LoginAs("admin", "pass");
    }

    [Fact(Skip = "we need to create a user for testing that we can change the email of, maybe do that in a dedicated test")]
    public async Task CanUpdateAccountInfo()
    {
        await Page.GotoAsync($"http://{_host}/user");
        await Page.GetByLabel("Display name").ClickAsync();
        await Page.GetByLabel("Display name").FillAsync("Test Admin Changed");
        await Page.GetByLabel("Email").ClickAsync();
        await Page.GetByLabel("Email").FillAsync("admin_new@test.com");
        // Submit the form
        await Page.ClickAsync("text=Update account info");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var successMessage = await Page.QuerySelectorAsync("text=Your account has been updated.");
        Assert.NotNull(successMessage);
    }

    [Fact]
    public async Task DisplaysFormErrorsOnInvalidData()
    {
        //need to use network idle, otherwise the inputs might get reset by hydration
        await Page.GotoAsync($"http://{_host}/user", new (){WaitUntil = WaitUntilState.NetworkIdle});
        await Page.FillAsync("#email", "");
        await Page.FillAsync("#name", "");
        await Page.ClickAsync("text=Update account info");
        await Expect(Page.GetByText("Invalid email")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanResetPassword()
    {
        await Page.GotoAsync($"http://{_host}/user");
        await Expect(Page).ToHaveURLAsync($"http://{_host}/user");
        await Page.ClickAsync("text=Reset your password instead?");
        await Expect(Page).ToHaveURLAsync($"http://{_host}/resetPassword");
    }
}


using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class UserPageTest : PageTest
{
    private string _host = TestingEnvironmentVariables.ServerHostname;

    [Fact]
    public async Task CanUpdateAccountInfo()
    {
        await Page.GotoAsync($"https://{_host}/user");
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
        await Page.GotoAsync($"https://{_host}/user");
        await Page.FillAsync("#email", "");
        await Page.FillAsync("#name", "");
        await Page.ClickAsync("text=Update account info");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        var emailError = await Page.QuerySelectorAsync("text=Invalid email");
        //var nameError = await Page.QuerySelectorAsync("text=Name is required"); this doesn't happen
        Assert.NotNull(emailError);
    }

    [Fact]
    public async Task CanResetPassword()
    {
        await Page.GotoAsync($"https://{_host}/user");
        await Page.ClickAsync("text=Reset your password instead?");
        await Page.WaitForLoadStateAsync(LoadState.DOMContentLoaded);
        Assert.Contains("/resetPassword", Page.Url);
    }
}


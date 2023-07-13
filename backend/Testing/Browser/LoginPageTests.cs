using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class LoginPageTests: PageTest
{
    private string _host = "staging.languagedepot.org";
    [Fact]
    public async Task CanLogin()
    {
        await Page.GotoAsync($"https://{_host}/login");

        await Page.GetByLabel("Email (or Send/Receive username)").ClickAsync();
        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("admin");
        await Page.GetByLabel("Password").FillAsync("pass");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/admin");
    }

    [Fact]
    public async Task ShowErrorWithoutUsername()
    {
        await Page.GotoAsync($"https://{_host}/login");

        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/login");
        await Expect(Page.GetByText("User info missing")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CanLoginAfterError()
    {
        await Page.GotoAsync($"https://{_host}/login");

        await Page.GetByLabel("Email (or Send/Receive username)").ClickAsync();
        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("admin");
        await Page.GetByLabel("Password").FillAsync("bad");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await  Expect(Page.GetByText("Something went wrong, please make sure you have used the correct account informa")).ToBeVisibleAsync();

        await Page.GetByLabel("Password").FillAsync("pass");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/admin");
    }
}

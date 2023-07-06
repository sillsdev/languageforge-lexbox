using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class AdminPageTest: PageTest
{
    private string _host = TestingEnvironmentVariables.ServerHostname;
    [Fact]
    public async Task CanNavigateToProjectPage()
    {
        await Page.GotoAsync($"https://{_host}/admin");

        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("admin");
        await Page.GetByLabel("Password").FillAsync("pass");
        await Page.GetByLabel("Password").PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync($"https://{_host}/admin");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Sena 3" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/project/sena-3");
    }
}

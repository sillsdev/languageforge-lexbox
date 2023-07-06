using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Services;

namespace Testing.Browser;

public class ManagerPageTest: PageTest
{
    private string _host = "staging.languagedepot.org";

    [Fact]
    public async Task DisplayDashboardThenProjectPage()
    {
        await Page.GotoAsync($"https://{_host}/login");

        await Page.GetByLabel("Email (or Send/Receive username)").ClickAsync();
        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("manager");
        await Page.GetByLabel("Password").FillAsync("pass");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Log in" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/");
        await Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Projects" })).ToBeVisibleAsync();

        await Page.GetByRole(AriaRole.Link, new() { Name = "Sena 3" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync($"https://{_host}/project/sena-3");
        await Expect(Page.GetByText("Project Code: sena-3")).ToBeVisibleAsync();
    }
}

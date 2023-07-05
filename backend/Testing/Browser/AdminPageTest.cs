using Microsoft.Playwright;
using Testing.Browser.Base;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class AdminPageTest: PageTest
{
    [Fact]
    public async Task CanNavigateToProjectPage()
    {
        await Page.GotoAsync("https://staging.languagedepot.org/admin");

        await Page.GetByLabel("Email (or Send/Receive username)").ClickAsync();

        await Page.GetByLabel("Email (or Send/Receive username)").FillAsync("admin");

        await Page.GetByLabel("Email (or Send/Receive username)").PressAsync("Tab");

        await Page.GetByLabel("Password").FillAsync("pass");

        await Page.GetByLabel("Password").PressAsync("Enter");

        await Expect(Page).ToHaveURLAsync("https://staging.languagedepot.org/admin");

        await Page.GetByRole(AriaRole.Link, new() { Name = "Sena 3" }).ClickAsync();

        await Expect(Page).ToHaveURLAsync("https://staging.languagedepot.org/project/sena-3");

    }
}

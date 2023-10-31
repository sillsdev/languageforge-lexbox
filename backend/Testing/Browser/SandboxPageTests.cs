using Microsoft.Playwright;
using Testing.Browser.Base;
using Testing.Browser.Page;

namespace Testing.Browser;

[Trait("Category", "Integration")]
public class SandboxPageTests : PageTest
{
    [Fact]
    public async Task CatchGoto500InSameTab()
    {

        await new SandboxPage(Page).Goto();
        await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByText("Goto 500 page").ClickAsync();
        }, "/api/testing/test500NoException");
        ExpectDeferredException();
    }

    [Fact]
    public async Task CatchGoto500InNewTab()
    {
        await new SandboxPage(Page).Goto();
        await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("goto 500 new tab").ClickAsync();
        });
        ExpectDeferredException();
    }

    [Fact(Skip = "Playwright doesn't catch the document load request of pages opened with Ctrl+Click")]
    public async Task CatchGoto500InNewTabWithCtrl()
    {
        await new SandboxPage(Page).Goto();
        await Context.RunAndWaitForPageAsync(async () =>
        {
            await Page.GetByText("Goto 500 page").ClickAsync(new()
            {
                Modifiers = new[] { KeyboardModifier.Control },
            });
        });
        ExpectDeferredException();
    }

    [Fact]
    public async Task CatchFetch500()
    {
        await new SandboxPage(Page).Goto();
        await Page.RunAndWaitForResponseAsync(async () =>
        {
            await Page.GetByText("Fetch 500").ClickAsync();
        }, "/api/testing/test500NoException");
        ExpectDeferredException();
        await Expect(Page.Locator(".modal-box.bg-error:has-text('Internal Server Error (500)')")).ToBeVisibleAsync();
    }
}

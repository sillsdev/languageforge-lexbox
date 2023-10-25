using Shouldly;
using Testing.Browser.Base;
using Testing.Browser.Page;

namespace Testing.Browser;

public class SandboxPageTests : PageTest
{
    [Fact]
    public async Task Goto500Works()
    {
        await new SandboxPage(Page).Goto();
        var request = await Page.RunAndWaitForRequestFinishedAsync(async () =>
        {
            await Page.GetByText("goto 500 page").ClickAsync();
        });
        var response = await request.ResponseAsync();
        response.ShouldNotBeNull();
        response.Ok.ShouldBeFalse();
        response.Status.ShouldBe(500);
    }
}

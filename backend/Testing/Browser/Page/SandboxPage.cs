using Microsoft.Playwright;

namespace Testing.Browser.Page;

public class SandboxPage : BasePage<SandboxPage>
{
    public SandboxPage(IPage page) : base(page, "/sandbox", page.Locator(":text('Sandbox')"))
    {
    }
}

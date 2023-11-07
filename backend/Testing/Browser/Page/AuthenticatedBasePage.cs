using Microsoft.Playwright;
using Testing.Browser.Component;

namespace Testing.Browser.Page;

public class AuthenticatedBasePage<T> : BasePage<T> where T : AuthenticatedBasePage<T>
{
    public EmailVerificationAlert EmailVerificationAlert { get; }
    public AuthenticatedBasePage(IPage page, string url, ILocator testLocator)
    : base(page, url, new[] { page.Locator("button .i-mdi-account-circle"), testLocator })
    {
        EmailVerificationAlert = new EmailVerificationAlert(Page);
    }

    public async Task GoHome()
    {
        await Page.Locator(".breadcrumbs").GetByRole(AriaRole.Link, new() { Name = "Home" }).ClickAsync();
    }
}

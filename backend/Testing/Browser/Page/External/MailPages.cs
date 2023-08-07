using Microsoft.Playwright;

namespace Testing.Browser.Page.External;

public interface MailInboxPage
{
    public string AddressId { get; }
    public ILocator EmailLocator { get; }

    public Task<MailInboxPage> GotoMailbox(string mailboxId);

    public Task<MailEmailPage> OpenEmail(int index = 0);
}

public abstract class MailEmailPage : BasePage<MailEmailPage>
{
    protected readonly ILocator bodyLocator;

    public MailEmailPage(IPage page, string? url, ILocator bodyLocator) : base(page, url, bodyLocator)
    {
        this.bodyLocator = bodyLocator;
    }

    public Task ClickVerifyEmail()
    {
        return bodyLocator.GetByRole(AriaRole.Link, new() { Name = "Verify e-mail" }).ClickAsync();
    }

    public Task ClickResetPassword()
    {
        return bodyLocator.GetByRole(AriaRole.Link, new() { Name = "Reset password" }).ClickAsync();
    }
}

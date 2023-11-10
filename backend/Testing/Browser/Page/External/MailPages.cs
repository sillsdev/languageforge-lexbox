using Microsoft.Playwright;
using Testing.Services;

namespace Testing.Browser.Page.External;

public abstract class MailInboxPage : BasePage<MailInboxPage>
{
    public string MailboxId { get; private set; }
    public ILocator EmailLocator { get; }

    public static MailInboxPage Get(IPage page, string mailboxId)
    {
        return TestingEnvironmentVariables.IsDev
            ? new MailDevInboxPage(page, mailboxId)
            : new MailinatorInboxPage(page, mailboxId);
    }

    public MailInboxPage(IPage page, string url, ILocator testLocator, string mailboxId, ILocator emailLocator)
    : base(page, url, testLocator)
    {
        MailboxId = mailboxId;
        EmailLocator = emailLocator;
    }

    protected abstract MailEmailPage GetEmailPage();

    public async Task<MailInboxPage> GotoMailbox(string mailboxId)
    {
        MailboxId = mailboxId;
        return await Goto();
    }

    public async Task<MailEmailPage> OpenEmail(int index = 0)
    {
        await EmailLocator.Nth(index).ClickAsync();
        return await GetEmailPage().WaitFor();
    }
}

public abstract class MailEmailPage : BasePage<MailEmailPage>
{
    protected readonly ILocator bodyLocator;

    public ILocator ResetPasswordButton => bodyLocator.GetByRole(AriaRole.Link, new() { Name = "Reset password" });

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
        return ResetPasswordButton.ClickAsync();
    }

    public virtual Task<string> GetFirstLanguageDepotUrl()
    {
        return bodyLocator.Locator($"a[href*='{TestingEnvironmentVariables.ServerBaseUrl}']").First.GetAttributeAsync("href");
    }
}

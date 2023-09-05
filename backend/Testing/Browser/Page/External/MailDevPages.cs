using Microsoft.Playwright;

namespace Testing.Browser.Page.External;

public class MailDevInboxPage : MailInboxPage
{
    public MailDevInboxPage(IPage page, string mailboxId)
    : base(page, "http://localhost:1080/#/", page.Locator("ul.email-list"), mailboxId, page.Locator("ul.email-list li a"))
    {
    }

    protected override MailEmailPage GetEmailPage()
    {
        return new MailDevEmailPage(Page);
    }

    public override async Task<MailInboxPage> Goto()
    {
        await base.Goto();
        await Page.Locator("input.search-input").FillAsync(MailboxId);
        return this;
    }
}

public class MailDevEmailPage : MailEmailPage
{
    public MailDevEmailPage(IPage page) : base(page,
        null, page.FrameLocator(".preview-iframe:visible").Locator("body"))
    {
    }
}

using Microsoft.Playwright;

namespace Testing.Browser.Page.External;

public class MailinatorInboxPage : MailInboxPage
{
    public MailinatorInboxPage(IPage page, string mailboxId)
    : base(page, $"https://www.mailinator.com/v4/public/inboxes.jsp?to={mailboxId}", page.Locator($"[id^='row_']").First,
    mailboxId, page.Locator($"[id^='row_']"))
    {
    }

    protected override MailEmailPage GetEmailPage()
    {
        return new MailinatorEmailPage(Page);
    }

    public override async Task<MailInboxPage> Goto()
    {
        Url = $"https://www.mailinator.com/v4/public/inboxes.jsp?to={MailboxId}";
        return await base.Goto();
    }
}

public class MailinatorEmailPage : MailEmailPage
{
    public MailinatorEmailPage(IPage page) : base(page,
        null, page.FrameLocator("#html_msg_body").Locator("body"))
    {
    }
}

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

    public override async Task<MailInboxPage> Goto(GotoOptions? options = null)
    {
        Url = $"https://www.mailinator.com/v4/public/inboxes.jsp?to={MailboxId}";
        return await base.Goto(options);
    }
}

public class MailinatorEmailPage : MailEmailPage
{
    public MailinatorEmailPage(IPage page) : base(page,
        null, page.FrameLocator("#html_msg_body").Locator("body"))
    {
    }

    public override async Task<string> GetFirstLanguageDepotUrl()
    {
        // Mailinator sometimes swaps links out with its own that and redirect to the original,
        // but the originals are made available in the links tab, which is always in the DOM
        return await Page.Locator("a[href*='jwt=']").First.GetAttributeAsync("href");
    }
}

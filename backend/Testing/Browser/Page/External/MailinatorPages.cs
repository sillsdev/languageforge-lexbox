using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page.External;

public class MailinatorInboxPage : BasePage<MailinatorInboxPage>, MailInboxPage
{
    public string AddressId { get; }
    public ILocator EmailLocator { get; private set; }

    public MailinatorInboxPage(IPage page, string mailboxId) : base(page,
        $"https://www.mailinator.com/v4/public/inboxes.jsp?to={mailboxId}",
        page.Locator($"[id^='row_']").First)
    {
        AddressId = mailboxId;
        EmailLocator = Page.Locator($"[id^='row_{mailboxId}']");
    }

    public async Task<MailInboxPage> GotoMailbox(string mailboxId)
    {
        EmailLocator = Page.Locator($"[id^='row_{mailboxId}']");
        return await Goto($"https://www.mailinator.com/v4/public/inboxes.jsp?to={mailboxId}");
    }

    public async Task<MailEmailPage> OpenEmail(int index = 0)
    {
        return await TaskUtil.WhenAllTakeFirst(
            new MailinatorEmailPage(Page).WaitFor(),
            EmailLocator.Nth(index).ClickAsync());
    }
}

public class MailinatorEmailPage : MailEmailPage
{
    public MailinatorEmailPage(IPage page) : base(page,
        null, page.FrameLocator("#html_msg_body").Locator("body"))
    {
    }
}

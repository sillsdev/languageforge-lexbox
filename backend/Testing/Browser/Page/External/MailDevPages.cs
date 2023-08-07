using Microsoft.Playwright;
using Testing.Browser.Util;

namespace Testing.Browser.Page.External;

public class MailDevInboxPage : BasePage<MailDevInboxPage>, MailInboxPage
{
    public string AddressId { get; private set; }
    public ILocator EmailLocator { get; private set; }

    public MailDevInboxPage(IPage page, string mailboxId) : base(page,
        "http://localhost:1080/#/", page.Locator("ul.email-list"))
    {
        AddressId = mailboxId;
        EmailLocator = Page.Locator("ul.email-list li a");
    }

    public override async Task<MailDevInboxPage> Goto()
    {
        await base.Goto();
        await Page.Locator("input.search-input").FillAsync(AddressId);
        return this;
    }

    public async Task<MailInboxPage> GotoMailbox(string mailboxId)
    {
        AddressId = mailboxId;
        return await Goto();
    }

    public async Task<MailEmailPage> OpenEmail(int index = 0)
    {
        return await TaskUtil.WhenAllTakeFirst(
            new MailDevEmailPage(Page).WaitFor(),
            EmailLocator.Nth(index).ClickAsync());
    }
}

public class MailDevEmailPage : MailEmailPage
{
    public MailDevEmailPage(IPage page) : base(page,
        null, page.FrameLocator(".preview-iframe:visible").Locator("body"))
    {
    }
}

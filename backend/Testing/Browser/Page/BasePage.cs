using Microsoft.Playwright;
using Shouldly;

namespace Testing.Browser.Page;

public abstract class BasePage<T> where T : BasePage<T>
{
    public IPage Page { get; private set; }
    public string? Url { get; private set; }
    protected ILocator[] TestLocators { get; }

    public BasePage(IPage page, string? url, ILocator testLocator)
    : this(page, url, new[] { testLocator })
    { }

    public BasePage(IPage page, string? url, ILocator[] testLocators)
    {
        Page = page;
        Url = url;
        TestLocators = testLocators;
    }

    public virtual async Task<T> Goto()
    {
        if (Url is null)
        {
            throw new NotSupportedException("Can't explicitly navigate to page, because it doesn't have a configured url.");
        }

        var response = await Page.GotoAsync(Url);
        response?.Ok.ShouldBeTrue(); // is null if same URL, but different hash
        await WaitFor();
        return (T)this;
    }

    public async Task<T> WaitFor()
    {
        await Task.WhenAll(
            Url is null ? Task.CompletedTask : Page.WaitForURLAsync(Url, new() { WaitUntil = WaitUntilState.NetworkIdle }),
            Task.WhenAll(TestLocators.Select(l => l.WaitForAsync()))
        );
        return (T)this;
    }

    protected async Task<T> Goto(string newUrl)
    {
        Url = newUrl;
        return await Goto();
    }
}

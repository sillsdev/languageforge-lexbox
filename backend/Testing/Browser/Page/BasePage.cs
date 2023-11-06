using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;

namespace Testing.Browser.Page;

public abstract class BasePage<T> where T : BasePage<T>
{
    public IPage Page { get; private set; }
    public string? Url { get; protected set; }
    protected ILocator[] TestLocators { get; }
    private Regex? UrlPattern => Url is not null ? new Regex($"{Regex.Escape(Url)}($|\\?|#)") : null;

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
        return await WaitFor();
    }

    public async Task<T> WaitFor()
    {
        if (UrlPattern is not null)
        {
            await Page.WaitForURLAsync(UrlPattern, new() { WaitUntil = WaitUntilState.Load });
        }
        else
        {
            await Page.WaitForLoadStateAsync(LoadState.Load);
        }
        await Task.WhenAll(TestLocators.Select(l => l.WaitForAsync()));
        return (T)this;
    }
}

using System.Text.RegularExpressions;
using Microsoft.Playwright;
using Shouldly;

namespace Testing.Browser.Page;

public record GotoOptions(bool? ExpectRedirect = false);

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

    public virtual async Task<T> Goto(GotoOptions? options = null)
    {
        if (Url is null)
        {
            throw new NotSupportedException("Can't explicitly navigate to page, because it doesn't have a configured url.");
        }

        var response = await Page.GotoAsync(Url);
        response?.Ok.ShouldBeTrue(); // is null if same URL, but different hash

        if (options?.ExpectRedirect != true)
        {
            await WaitFor();
        }

        return (T)this;
    }

    public async Task<T> WaitFor()
    {
        if (UrlPattern is not null)
        {
            //assert to get a good error message
            await Assertions.Expect(Page).ToHaveURLAsync(UrlPattern);
            //still wait to make sure we reach the same state we expect
            await Page.WaitForURLAsync(UrlPattern, new() { WaitUntil = WaitUntilState.Load });
        }
        else
        {
            await Page.WaitForLoadStateAsync(LoadState.Load);
        }
        await Task.WhenAll(TestLocators.Select(l => Assertions.Expect(l).ToBeVisibleAsync()));
        return (T)this;
    }
}

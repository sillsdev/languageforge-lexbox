using Microsoft.Playwright;
using Microsoft.Playwright.TestAdapter;

namespace Testing.Browser.Base;

public class PageTest : IAsyncLifetime
{
    private readonly PlaywrightFixture _fixture;
    public IPage Page => _fixture.Page;
    public IBrowser Browser => _fixture.Browser;
    public IBrowserContext Context => _fixture.Context;

    public PageTest()
    {
        _fixture = new PlaywrightFixture();
    }

    public ILocatorAssertions Expect(ILocator locator) => Assertions.Expect(locator);
    public IPageAssertions Expect(IPage page) => Assertions.Expect(page);
    public IAPIResponseAssertions Expect(IAPIResponse response) => Assertions.Expect(response);

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _fixture.DisposeAsync();
    }
}

public class PlaywrightFixture : IAsyncLifetime
{
    public async Task InitializeAsync()
    {
        PlaywrightInstance = await Playwright.CreateAsync();
        Browser = await PlaywrightInstance.Chromium.LaunchAsync();
        Context = await Browser.NewContextAsync(new BrowserNewContextOptions());
        Page = await Context.NewPageAsync();
    }

    public IPage Page { get; set; } = null!;
    public IBrowser Browser { get; set; } = null!;
    public IBrowserContext Context { get; set; } = null!;
    private IPlaywright PlaywrightInstance { get; set; } = null!;

    public async Task DisposeAsync()
    {
        await Browser.DisposeAsync();
        PlaywrightInstance.Dispose();
    }
}

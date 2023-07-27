using System.Net;
using System.Net.Http.Json;
using Microsoft.Playwright;
using Shouldly;
using Testing.Services;

namespace Testing.Browser.Base;

public class PageTest : IAsyncLifetime
{
    protected bool EnableTrace { get; init; }
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

    public virtual async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        if (EnableTrace)
        {
            await Context.Tracing.StartAsync(new ()
            {
                Screenshots = true,
                Snapshots = true,
                Sources = true
            });
        }
    }

    public virtual async Task DisposeAsync()
    {
        if (EnableTrace)
        {
            await Context.Tracing.StopAsync(new() { Path = "trace.zip" });
        }

        await _fixture.DisposeAsync();
    }

    static readonly HttpClient HttpClient = new HttpClient();

    public async Task LoginAs(string user, string password)
    {
        var responseMessage = await HttpClient.PostAsJsonAsync(
            $"http://{TestingEnvironmentVariables.ServerHostname}/api/login",
            new Dictionary<string, object>
            {
                { "password", password }, { "emailOrUsername", user }, { "preHashedPassword", false }
            });
        responseMessage.Headers.ToDictionary(kvp => kvp.Key, kvp => kvp.Value, StringComparer.OrdinalIgnoreCase)
            .ShouldContainKey("Set-Cookie");
        var cookies = responseMessage.Headers.GetValues("Set-Cookie").ToArray();
        cookies.ShouldNotBeEmpty();
        var cookieContainer = new CookieContainer();
        foreach (var cookie in cookies)
        {
            cookieContainer.SetCookies(new($"http://{TestingEnvironmentVariables.ServerHostname}"), cookie);
        }

        await Context.AddCookiesAsync(cookieContainer.GetAllCookies()
            .Select(cookie => new Microsoft.Playwright.Cookie
            {
                Value = cookie.Value,
                Domain = cookie.Domain,
                Expires = (float) cookie.Expires.Subtract(DateTime.UnixEpoch).TotalSeconds,
                Name = cookie.Name,
                Path = cookie.Path,
                Secure = cookie.Secure,
                HttpOnly = cookie.HttpOnly
            }));
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

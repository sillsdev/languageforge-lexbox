using System.Net.Http.Json;
using LexCore.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FwLiteShared.AppUpdate;

/// <summary>
/// Base implementation of <see cref="IPlatformUpdateService"/> that checks for updates via HTTP to LexBox/GitHub.
/// Used by Linux and Web. MAUI platforms extend <see cref="MauiPlatformUpdateServiceBase"/> instead.
/// </summary>
public class CorePlatformUpdateService : IPlatformUpdateService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<FwLiteConfig> _config;
    private readonly ILogger<CorePlatformUpdateService> _logger;

    public CorePlatformUpdateService(
        IHttpClientFactory httpClientFactory,
        IOptions<FwLiteConfig> config,
        ILogger<CorePlatformUpdateService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _config = config;
        _logger = logger;
    }

    public virtual DateTime LastUpdateCheck
    {
        get => DateTime.MinValue;
        set { /* no-op, does not persist last update time */ }
    }

    public virtual bool IsOnMeteredConnection() => false;

    public virtual bool SupportsAutoUpdate => false;

    public virtual async Task<ShouldUpdateResponse> ShouldUpdateAsync()
    {
        try
        {
            var response = await _httpClientFactory
                .CreateClient("Lexbox")
                .SendAsync(new HttpRequestMessage(HttpMethod.Get, _config.Value.UpdateUrl)
                {
                    Headers = { { "User-Agent", $"Fieldworks-Lite-Client/{_config.Value.AppVersion}" } }
                });

            if (!response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogError("Failed to get should update response: {StatusCode} {ResponseContent}",
                    response.StatusCode, responseContent);
                return new ShouldUpdateResponse(null);
            }

            var result = await response.Content.ReadFromJsonAsync<ShouldUpdateResponse>();
            return result ?? new ShouldUpdateResponse(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch latest release");
            return new ShouldUpdateResponse(null);
        }
    }

    public virtual Task<UpdateResult> ApplyUpdate(FwLiteRelease latestRelease)
    {
        return Task.FromResult(UpdateResult.ManualUpdateRequired);
    }

    public virtual Task<bool> RequestPermissionToUpdate(FwLiteRelease latestRelease)
    {
        return Task.FromResult(true);
    }
}

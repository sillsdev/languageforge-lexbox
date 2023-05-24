using System.Text;
using System.Text.Json;
using LexBoxApi.Config;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

public class TurnstileService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptionsSnapshot<CloudFlareConfig> _options;

    public TurnstileService(IHttpClientFactory httpClientFactory, IOptionsSnapshot<CloudFlareConfig> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public async Task<bool> IsTokenValid(string token)
    {
        var httpClient = _httpClientFactory.CreateClient("cloudflare");
        var data = new StringContent(
            $"secret={_options.Value.TurnstileKey}&response={token}",
            Encoding.UTF8,
            "application/x-www-form-urlencoded"
        );
        var response = await httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", data);
        var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var success = (responseJson?.RootElement.TryGetProperty("success"u8, out var prop) ?? false) && prop.GetBoolean();
        return success;
    }
}
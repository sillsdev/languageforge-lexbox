using System.Text;
using System.Text.Json;
using LexBoxApi.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Services;

public class TurnstileService(IHttpClientFactory httpClientFactory, IOptionsSnapshot<CloudFlareConfig> options)
{
    public async Task<bool> IsTokenValid(string token, string? email = null)
    {
        if (email is not null)
        {
            var allowDomain = options.Value.AllowDomain;
            if (!string.IsNullOrEmpty(allowDomain) && email.EndsWith($"@{allowDomain}"))
            {
                return true;
            }
        }

        var httpClient = httpClientFactory.CreateClient("cloudflare");


        var data = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            { "secret", options.Value.TurnstileKey }, { "response", token }
        });
        var response = await httpClient.PostAsync("https://challenges.cloudflare.com/turnstile/v0/siteverify", data);
        var responseJson = await response.Content.ReadFromJsonAsync<JsonDocument>();
        var success = (responseJson?.RootElement.TryGetProperty("success"u8, out var prop) ?? false) && prop.GetBoolean();
        return success;
    }
}

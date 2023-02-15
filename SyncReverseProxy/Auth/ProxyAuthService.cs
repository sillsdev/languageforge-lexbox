using Microsoft.Extensions.Options;
using WebApi.Config;

namespace WebApi.Auth;

public class ProxyAuthService
{
    private readonly IHttpClientFactory _clientFactory;
    private readonly LexBoxApiConfig _lexBoxApiConfig;

    public ProxyAuthService(IHttpClientFactory clientFactory, IOptionsSnapshot<LexBoxApiConfig> optionsSnapshot)
    {
        _clientFactory = clientFactory;
        _lexBoxApiConfig = optionsSnapshot.Value;
    }
    
    public async Task<bool> IsAuthorized(string userName, string password)
    {
        var client = _clientFactory.CreateClient("admin");
        var response = await client.PostAsync($"{_lexBoxApiConfig.Url}/api/user/{userName}/password",
            new FormUrlEncodedContent(
                new[]
                {
                    new KeyValuePair<string, string>("password", password)
                }));
        return response.IsSuccessStatusCode;
    }
}
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.Auth;
public class GoogleTokenValidator(IOptions<GoogleOptions> options, IHttpClientFactory clientFactory)
{
    private readonly ConfigurationManager<OpenIdConnectConfiguration> openIdConfigManager = new(
            "https://accounts.google.com/.well-known/openid-configuration",
            new OpenIdConnectConfigurationRetriever(),
            new HttpDocumentRetrieverFactory(clientFactory));

    public async Task<ClaimsIdentity?> ValidateGoogleJwt(string jwt)
    {
        var discoveryDocument = await openIdConfigManager.GetConfigurationAsync();
        var validationParameters = new TokenValidationParameters
        {
            IssuerSigningKeys = discoveryDocument.SigningKeys,
            ValidateIssuerSigningKey = true,
            ValidIssuer = discoveryDocument.Issuer,
            ValidAudience = options.Value.ClientId,
            ValidateLifetime = true,
            RequireExpirationTime = true
        };
        var tokenHandler = new JwtSecurityTokenHandler();
        TokenValidationResult validationResult = await tokenHandler.ValidateTokenAsync(jwt, validationParameters);
        return validationResult.IsValid ? validationResult.ClaimsIdentity : null;
    }

    private class HttpDocumentRetrieverFactory(IHttpClientFactory httpClientFactory) : IDocumentRetriever
    {
        private HttpDocumentRetriever internalRetriever => new(httpClientFactory.CreateClient());

        public Task<string> GetDocumentAsync(string address, CancellationToken cancel)
        {
            return internalRetriever.GetDocumentAsync(address, cancel);
        }
    }
}

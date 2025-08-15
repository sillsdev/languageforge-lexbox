namespace FwLiteShared.Auth;

public interface IRedirectUrlProvider
{
    string? GetRedirectUrl();
    bool ShouldRecreateAuthHelper(string? redirectUrl);
}

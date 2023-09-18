using LexCore.Auth;
using LexSyncReverseProxy;

namespace LexCore.ServiceInterfaces;

public interface ILexProxyService
{
    Task<LexAuthUser?> Login(LoginRequest loginRequest);
    Task RefreshProjectLastChange(string projectCode);
    ValueTask<RequestInfo> GetDestinationPrefix(HgType type, string projectCode);
}

public record RequestInfo(string DestinationPrefix, string? TrustToken);

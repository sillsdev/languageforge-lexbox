using LexCore.Auth;
using LexSyncReverseProxy;

namespace LexCore.ServiceInterfaces;

public interface ILexProxyService
{
    Task<LexAuthUser?> Login(LoginRequest loginRequest);
    Task RefreshProjectLastChange(string projectCode);
    ValueTask<string> GetDestinationPrefix(HgType type, string projectCode);
}

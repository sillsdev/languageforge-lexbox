using LexCore.Auth;
using LexCore.Entities;
using LexSyncReverseProxy;

namespace LexCore.ServiceInterfaces;

public interface ILexProxyService
{
    Task<LexAuthUser?> Login(LoginRequest loginRequest);
    Task RefreshProjectLastChange(string projectCode);
    Task UpdateLastEntryCountIfAllowed(string projectCode);
    RequestInfo GetDestinationPrefix(HgType type);
}

public record RequestInfo(string DestinationPrefix);

using LexCore.Auth;

namespace LexCore.ServiceInterfaces;

public interface ILexProxyService
{
    Task<LexAuthUser?> Login(LoginRequest loginRequest);
    Task RefreshProjectLastChange(string projectCode);
}
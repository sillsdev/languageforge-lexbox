using System.Security.Claims;

namespace LexCore.ServiceInterfaces;

public interface ILexProxyService
{
    Task<ClaimsPrincipal?> Login(LoginRequest loginRequest);
    Task RefreshProjectLastChange(string projectCode);
}
using System.Security.Claims;

namespace LexCore.ServiceInterfaces;

public interface IProxyAuthService
{
    Task<ClaimsPrincipal?> Login(string userName, string password);
}
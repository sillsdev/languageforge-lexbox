namespace LexCore.ServiceInterfaces;

public interface IProxyAuthService
{
    Task<bool> IsAuthorized(string userName, string password);
}
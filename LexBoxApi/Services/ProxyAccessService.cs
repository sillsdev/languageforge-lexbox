using LexCore;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class ProxyAuthService : IProxyAuthService
{
    private readonly LexBoxDbContext _lexBoxDbContext;

    public ProxyAuthService(LexBoxDbContext lexBoxDbContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
    }

    public async Task<bool> IsAuthorized(string userName, string password)
    {
        var user = await _lexBoxDbContext.Users.SingleOrDefaultAsync(u => u.Username == userName);
        if (user == null)
        {
            return false;
        }

        return PasswordHashing.RedminePasswordHash(password, user.Salt) == user.PasswordHash;
    }
}
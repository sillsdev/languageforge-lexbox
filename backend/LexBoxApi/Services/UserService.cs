using LexBoxApi.Auth;
using LexCore.Auth;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class UserService(LexBoxDbContext dbContext, EmailService emailService, LexAuthService lexAuthService)
{
    public async Task ForgotPassword(string email)
    {
        var (lexAuthUser, user) = await lexAuthService.GetUser(email);
        // we want to silently return if the user doesn't exist, so we don't leak information.
        if (lexAuthUser is null || user?.CanLogin() is not true) return;
        var (jwt, _) = lexAuthService.GenerateJwt(lexAuthUser, audience: LexboxAudience.ForgotPassword);
        await emailService.SendForgotPasswordEmail(jwt, user);
    }
    public async Task UpdateUserLastActive(Guid id)
    {
        await dbContext.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(c => c.SetProperty(u => u.LastActive, DateTimeOffset.UtcNow));
    }

    public async Task<long> GetUserUpdatedDate(Guid id)
    {
        return (await dbContext.Users.Where(u => u.Id == id)
            .Select(u => u.UpdatedDate)
            .SingleOrDefaultAsync()).ToUnixTimeSeconds();
    }
}

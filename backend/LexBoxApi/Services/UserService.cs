using LexBoxApi.Auth;
using LexBoxApi.Services.Email;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class UserService(LexBoxDbContext dbContext, IEmailService emailService, LexAuthService lexAuthService)
{
    public async Task ForgotPassword(string email)
    {
        await emailService.SendForgotPasswordEmail(email);
    }
    public async Task UpdateUserLastActive(Guid id)
    {
        await dbContext.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(c => c.SetProperty(u => u.LastActive, DateTimeOffset.UtcNow));
    }

    public async Task UpdatePasswordStrength(Guid id, LexCore.LoginRequest loginRequest)
    {
        var canCalculateScore = !loginRequest.PreHashedPassword;
        var hasScore = loginRequest.PasswordStrength != null;
        if ((canCalculateScore || hasScore) &&
            await dbContext.Users.AnyAsync(u => u.Id == id && u.PasswordStrength == null))
        {
            var score =
                hasScore ? loginRequest.PasswordStrength :
                canCalculateScore ? Zxcvbn.Core.EvaluatePassword(loginRequest.Password).Score :
                null;
            if (score is not null and >= 0 and <= 4)
            {
                await dbContext.Users.Where(u => u.Id == id)
                    .ExecuteUpdateAsync(c => c.SetProperty(u => u.PasswordStrength, score));
            }
        }
    }

    public static int? ClampPasswordStrength(int? strength)
    {
        var clamped = strength;
        if (clamped is not null and < 0) clamped = 0;
        if (clamped is not null and > 4) clamped = 4;
        return clamped;
    }

    public async Task<long> GetUserUpdatedDate(Guid id)
    {
        return (await dbContext.Users.Where(u => u.Id == id)
            .Select(u => u.UpdatedDate)
            .SingleOrDefaultAsync()).ToUnixTimeSeconds();
    }
}

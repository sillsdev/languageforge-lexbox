using LexBoxApi.Auth;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class UserService(LexBoxDbContext dbContext, EmailService emailService, LexAuthService lexAuthService)
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

    public async Task<long> GetUserUpdatedDate(Guid id)
    {
        return (await dbContext.Users.Where(u => u.Id == id)
            .Select(u => u.UpdatedDate)
            .SingleOrDefaultAsync()).ToUnixTimeSeconds();
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await dbContext.Users.Where(u => u.Email == email).SingleOrDefaultAsync();
        // TODO: Add && u.EmailVerified to the above?
    }
}

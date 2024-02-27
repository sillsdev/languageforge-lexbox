﻿using LexBoxApi.Auth;
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

    public async Task UpdatePasswordStrength(LexCore.Entities.User user, LexCore.LoginRequest loginRequest)
    {
        if (user.PasswordStrength is null && !loginRequest.PreHashedPassword)
        {
            var strength = Zxcvbn.Core.EvaluatePassword(loginRequest.Password);
            user.PasswordStrength = strength.Score;
            await dbContext.SaveChangesAsync();
        }
    }

    public async Task<long> GetUserUpdatedDate(Guid id)
    {
        return (await dbContext.Users.Where(u => u.Id == id)
            .Select(u => u.UpdatedDate)
            .SingleOrDefaultAsync()).ToUnixTimeSeconds();
    }
}

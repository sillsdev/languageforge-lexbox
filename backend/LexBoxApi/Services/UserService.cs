using System.Net.Mail;
using LexBoxApi.Services.Email;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class UserService(LexBoxDbContext dbContext, IEmailService emailService)
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

    /// <summary>
    /// returns LexAuthUser.NewUserUpdatedTimestamp if the user does not exist
    /// </summary>
    /// <param name="id"></param>
    /// <returns></returns>
    public async Task<long> GetUserUpdatedDate(Guid id)
    {
        return (await dbContext.Users.Where(u => u.Id == id)
            .Select(u => (DateTimeOffset?) u.UpdatedDate)
            .SingleOrDefaultAsync())?.ToUnixTimeSeconds() ?? LexAuthUser.NewUserUpdatedTimestamp;
    }

    public static (string name, string? email, string? username) ExtractNameAndAddressFromUsernameOrEmail(string usernameOrEmail)
    {
        var isEmailAddress = usernameOrEmail.Contains('@');
        string name;
        string? email;
        string? username;
        if (isEmailAddress)
        {
            try
            {
                var parsed = new MailAddress(usernameOrEmail);
                email = parsed.Address;
                username = null;
                name = parsed.DisplayName;
                if (string.IsNullOrEmpty(name)) name = email.Split('@')[0];
            }
            catch (FormatException)
            {
                // FormatException message from .NET talks about mail headers, which is confusing here
                throw new InvalidEmailException("Invalid email address", usernameOrEmail);
            }
        }
        else
        {
            username = usernameOrEmail;
            email = null;
            name = username;
        }
        return (name, email, username);
    }

    public IQueryable<User> UserQueryForTypeahead(LexAuthUser user)
    {
        var myOrgIds = user.Orgs.Select(o => o.OrgId).ToList();
        var myProjectIds = user.Projects.Select(p => p.ProjectId).ToList();
        var myManagedProjectIds = user.Projects.Where(p => p.Role == ProjectRole.Manager).Select(p => p.ProjectId).ToList();
        return dbContext.Users.AsNoTracking().Where(u =>
            u.Id == user.Id ||
            u.Organizations.Any(orgMember => myOrgIds.Contains(orgMember.OrgId)) ||
            u.Projects.Any(projMember =>
                myManagedProjectIds.Contains(projMember.ProjectId) ||
                (projMember.Project != null && projMember.Project.IsConfidential != true && myProjectIds.Contains(projMember.ProjectId))));
    }
}

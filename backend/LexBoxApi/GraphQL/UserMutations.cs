using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.GraphQL;

[MutationType]
public class UserMutations
{
    public record ChangeUserAccountDataInput(Guid UserId, [property: EmailAddress] string Email, string Name);
    public record ChangeUserAccountByAdminInput(Guid UserId, string Email, string Name, UserRole Role)
        : ChangeUserAccountDataInput(UserId, Email, Name);

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<InvalidFormatException>]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [UseMutationConvention]
    [RefreshJwt]
    public Task<User> ChangeUserAccountData(
        LoggedInContext loggedInContext,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        if (loggedInContext.User.Id != input.UserId) throw new UnauthorizedAccessException();
        return UpdateUser(loggedInContext, input, dbContext, emailService, lexAuthService);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<InvalidFormatException>]
    [AdminRequired]
    public Task<User> ChangeUserAccountByAdmin(
        LoggedInContext loggedInContext,
        ChangeUserAccountByAdminInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        return UpdateUser(loggedInContext, input, dbContext, emailService, lexAuthService);
    }

    private static async Task<User> UpdateUser(
        LoggedInContext loggedInContext,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is null) throw new NotFoundException("User not found");

        if (!input.Name.IsNullOrEmpty())
        {
            user.Name = input.Name;
        }

        if (input is ChangeUserAccountByAdminInput adminInput)
        {
            loggedInContext.User.AssertIsAdmin();
            if (user.Id != loggedInContext.User.Id)
            {
                user.IsAdmin = adminInput.Role == UserRole.admin;
            }
        }

        await dbContext.SaveChangesAsync();

        if (!input.Email.IsNullOrEmpty() && !input.Email.Equals(user.Email))
        {
            await SendVerifyNewAddressEmail(user, emailService, lexAuthService, input.Email);
        }

        return user;
    }

    private static async Task SendVerifyNewAddressEmail(
        User user,
        EmailService emailService,
        LexAuthService lexAuthService,
        string newEmail
    )
    {
        var (jwt, _) = lexAuthService.GenerateJwt(new LexAuthUser(user)
        {
            EmailVerificationRequired = null,
            Email = newEmail,
        });
        await emailService.SendVerifyAddressEmail(jwt, user, newEmail);
    }

    public record DeleteUserByAdminInput(Guid UserId);

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<User> DeleteUserByAdminOrSelf(DeleteUserByAdminOrSelfInput input, LexBoxDbContext dbContext, LoggedInContext loggedInContext)
    {
        loggedInContext.User.AssertCanDeleteAccount(input.UserId);
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is null) throw new NotFoundException("User not found");
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        return user;
    }
}

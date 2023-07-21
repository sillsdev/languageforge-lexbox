using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexBoxApi.Models.Project;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.GraphQL;

[MutationType]
public class UserMutations
{
    public record ChangeUserAccountDataInput(Guid UserId, [property: EmailAddress] string Email, string Name);

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<InvalidFormatException>]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [UseMutationConvention]
    public Task<User> ChangeUserAccountData(
        LoggedInContext loggedInContext,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        if (loggedInContext.User.Id != input.UserId) throw new UnauthorizedAccessException();
        return UpdateUser(input, dbContext, emailService, lexAuthService);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<InvalidFormatException>]
    [UseMutationConvention(InputTypeName = nameof(ChangeUserAccountDataInput))]
    [AdminRequired]
    public Task<User> ChangeUserAccountByAdmin(
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        return UpdateUser(input, dbContext, emailService, lexAuthService);
    }

    private static async Task<User> UpdateUser(
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

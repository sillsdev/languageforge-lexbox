using System.ComponentModel.DataAnnotations;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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
    [Error<UniqueValueException>]
    [UseMutationConvention]
    [RefreshJwt]
    public Task<User> ChangeUserAccountData(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        if (loggedInContext.User.Id != input.UserId) throw new UnauthorizedAccessException();
        return UpdateUser(loggedInContext, permissionService, input, dbContext, emailService, lexAuthService);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UniqueValueException>]
    [AdminRequired]
    public Task<User> ChangeUserAccountByAdmin(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        ChangeUserAccountByAdminInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        return UpdateUser(loggedInContext, permissionService, input, dbContext, emailService, lexAuthService);
    }

    private static async Task<User> UpdateUser(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
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
            permissionService.AssertIsAdmin();
            if (user.Id != loggedInContext.User.Id)
            {
                user.IsAdmin = adminInput.Role == UserRole.admin;
            }
        }
        user.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();

        if (!input.Email.IsNullOrEmpty() && !input.Email.Equals(user.Email, StringComparison.InvariantCultureIgnoreCase))
        {
            await SendVerifyNewAddressEmail(user, emailService, lexAuthService, dbContext, input.Email);
        }

        return user;
    }

    private static async Task SendVerifyNewAddressEmail(User user,
        EmailService emailService,
        LexAuthService lexAuthService,
        LexBoxDbContext lexBoxDbContext,
        string newEmail)
    {
        var emailInUse = await lexBoxDbContext.Users.AnyAsync(u => u.Email == newEmail);
        if (emailInUse) throw new UniqueValueException("Email");
        var (jwt, _) = lexAuthService.GenerateJwt(new LexAuthUser(user)
        {
            EmailVerificationRequired = null,
            Email = newEmail,
        });
        await emailService.SendVerifyAddressEmail(jwt, user, newEmail);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<User> DeleteUserByAdminOrSelf(DeleteUserByAdminOrSelfInput input, LexBoxDbContext dbContext, IPermissionService permissionService)
    {
        permissionService.AssertCanDeleteAccount(input.UserId);
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is null) throw new NotFoundException("User not found");
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        return user;
    }
}

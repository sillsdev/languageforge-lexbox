using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexBoxApi.GraphQL.CustomTypes;
using LexBoxApi.Models.Project;
using LexBoxApi.Otel;
using LexBoxApi.Services;
using LexCore;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.Exceptions;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace LexBoxApi.GraphQL;

[MutationType]
public class UserMutations
{
    public record ChangeUserAccountDataInput(Guid UserId, [property: EmailAddress] string? Email, string Name);
    public record ChangeUserAccountBySelfInput(Guid UserId, string? Email, string Name, string Locale)
        : ChangeUserAccountDataInput(UserId, Email, Name);
    public record ChangeUserAccountByAdminInput(Guid UserId, string? Email, string Name, UserRole Role)
        : ChangeUserAccountDataInput(UserId, Email, Name);
    public record CreateGuestUserByAdminInput(
        string? Email,
        string Name,
        string? Username,
        string Locale,
        string PasswordHash,
        int PasswordStrength);

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UniqueValueException>]
    [UseMutationConvention]
    [RefreshJwt]
    public async Task<MeDto> ChangeUserAccountBySelf(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        ChangeUserAccountBySelfInput input,
        LexBoxDbContext dbContext,
        EmailService emailService
    )
    {
        if (loggedInContext.User.Id != input.UserId) throw new UnauthorizedAccessException();
        var user = await UpdateUser(loggedInContext, permissionService, input, dbContext, emailService);
        return new MeDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Locale = user.LocalizationCode
        };
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
        EmailService emailService
    )
    {
        return UpdateUser(loggedInContext, permissionService, input, dbContext, emailService);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<UniqueValueException>]
    [Error<ProjectMemberInvitedByEmail>]
    [Error<RequiredException>]
    [AdminRequired]
    public async Task<LexAuthUser> CreateGuestUserByAdmin(
        LoggedInContext loggedInContext,
        CreateGuestUserByAdminInput input,
        LexBoxDbContext dbContext,
        EmailService emailService
    )
    {
        using var createGuestUserActivity = LexBoxActivitySource.Get().StartActivity("CreateGuestUser");

        var hasExistingUser = input.Email is null && input.Username is null
            ? throw new RequiredException("Guest users must have either an email or a username")
            : await dbContext.Users.FilterByEmailOrUsername(input.Email ?? input.Username!).AnyAsync();
        createGuestUserActivity?.AddTag("app.email_available", !hasExistingUser);
        if (hasExistingUser) throw new UniqueValueException("Email");

        var admin = loggedInContext.User;

        if (string.IsNullOrEmpty(input.Email))
        {
            var salt = Convert.ToHexString(RandomNumberGenerator.GetBytes(SHA1.HashSizeInBytes));
            var userEntity = new User
            {
                Id = Guid.NewGuid(),
                Name = input.Name,
                Email = input.Email,
                Username = input.Username,
                LocalizationCode = input.Locale,
                Salt = salt,
                PasswordHash = PasswordHashing.HashPassword(input.PasswordHash, salt, true),
                PasswordStrength = UserService.ClampPasswordStrength(input.PasswordStrength),
                IsAdmin = false,
                EmailVerified = false,
                CreatedById = admin.Id,
                Locked = false,
                CanCreateProjects = false
            };
            createGuestUserActivity?.AddTag("app.user.id", userEntity.Id);
            dbContext.Users.Add(userEntity);
            await dbContext.SaveChangesAsync();
            return new LexAuthUser(userEntity);
        }
        else
        {
            await emailService.SendCreateAccountEmail(input.Email, null, ProjectRole.Editor, admin.Name, null, input.Locale);
            throw new ProjectMemberInvitedByEmail("Invitation email sent");
        }
    }

    private static async Task<User> UpdateUser(
        LoggedInContext loggedInContext,
        IPermissionService permissionService,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext,
        EmailService emailService
    )
    {
        var user = await dbContext.Users.FindAsync(input.UserId);
        NotFoundException.ThrowIfNull(user);

        if (!input.Name.IsNullOrEmpty())
        {
            user.Name = input.Name;
        }

        bool wasPromotedToAdmin = false;
        if (input is ChangeUserAccountByAdminInput adminInput)
        {
            permissionService.AssertIsAdmin();
            if (user.Id != loggedInContext.User.Id)
            {
                if (!user.IsAdmin && adminInput.Role == UserRole.admin)
                {
                    if (!user.EmailVerified)
                    {
                        throw new ValidationException("User must have a verified email address to be promoted to admin");
                    }
                    wasPromotedToAdmin = user.IsAdmin = true;
                }
            }
        }
        else if (input is ChangeUserAccountBySelfInput selfInput)
        {
            if (!selfInput.Locale.IsNullOrEmpty())
            {
                user.LocalizationCode = selfInput.Locale;
            }
        }

        user.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();

        if (!string.IsNullOrEmpty(input.Email) && !input.Email.Equals(user.Email, StringComparison.InvariantCultureIgnoreCase))
        {
            var emailInUse = await dbContext.Users.AnyAsync(u => u.Email == input.Email);
            if (emailInUse) throw new UniqueValueException("Email");
            await emailService.SendVerifyAddressEmail(user, input.Email);
        }

        if (wasPromotedToAdmin)
        {
            var admins = dbContext.Users.Where(u => u.IsAdmin).AsAsyncEnumerable();
            ArgumentException.ThrowIfNullOrEmpty(user.Email);
            await emailService.SendNewAdminEmail(admins, user.Name, user.Email);
        }

        return user;
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    public async Task<User> DeleteUserByAdminOrSelf(DeleteUserByAdminOrSelfInput input, LexBoxDbContext dbContext, IPermissionService permissionService)
    {
        permissionService.AssertCanDeleteAccount(input.UserId);
        var user = await dbContext.Users.FindAsync(input.UserId);
        NotFoundException.ThrowIfNull(user);
        dbContext.Users.Remove(user);
        await dbContext.SaveChangesAsync();
        return user;
    }

    public record SetUserLockedInput(Guid UserId, bool Locked);

    [AdminRequired]
    [Error<NotFoundException>]
    [UseMutationConvention]
    public async Task<User> SetUserLocked(SetUserLockedInput input, LexBoxDbContext dbContext, IPermissionService permissionService)
    {
        permissionService.AssertCanLockOrUnlockUser(input.UserId);
        var user = await dbContext.Users.FindAsync(input.UserId);
        NotFoundException.ThrowIfNull(user);
        user.Locked = input.Locked;
        user.UpdateUpdatedDate();
        await dbContext.SaveChangesAsync();
        return user;
    }
}

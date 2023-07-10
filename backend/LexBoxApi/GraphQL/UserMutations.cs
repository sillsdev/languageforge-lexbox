using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
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
        return UpdateUser(input.UserId, input.Name, input.Email, dbContext, emailService, lexAuthService);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [Error<InvalidFormatException>]
    [UseMutationConvention]
    [AdminRequired]
    public Task<User> ChangeUserAccountByAdmin(
        ChangeUserAccountByAdminInput input,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        return UpdateUser(input.UserId, input.Name, input.Email, dbContext, emailService, lexAuthService);
    }

    private static async Task<User> UpdateUser(
        Guid userId,
        string name,
        string email,
        LexBoxDbContext dbContext,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        var user = await dbContext.Users.FindAsync(userId);
        if (user is null) throw new NotFoundException("User not found");
        if (!name.IsNullOrEmpty())
        {
            user.Name = name;
        }

        var emailChanged = UpdateUserEmail(user, email);

        await dbContext.SaveChangesAsync();

        if (emailChanged)
        {
            await SendVerifyAddressEmail(user, emailService, lexAuthService);
        }

        return user;
    }

    private static async Task SendVerifyAddressEmail(
        User user,
        EmailService emailService,
        LexAuthService lexAuthService
    )
    {
        var authUser = new LexAuthUser(user);
        var (jwt, _) = lexAuthService.GenerateJwt(authUser);
        await emailService.SendVerifyAddressEmail(jwt, user);
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [AdminRequired]
    public async Task<User> DeleteUserByAdmin(DeleteUserByAdminInput input, LexBoxDbContext dbContext)
    {
        var User = await dbContext.Users.FindAsync(input.UserId);
        var user = dbContext.Users.Where(u => u.Id == input.UserId);
        await user.ExecuteDeleteAsync();
        return User;
    }

    private static bool UpdateUserEmail(User user, string newEmail)
    {
        if (!newEmail.IsNullOrEmpty() && !newEmail.Equals(user.Email))
        {
            SimpleValidator.Email(newEmail);
            user.PreviousEmail = user.Email;
            user.Email = newEmail;
            return true;
        }
        return false;
    }
}

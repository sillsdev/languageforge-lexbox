﻿using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.GraphQL;

[MutationType]
public class UserMutations
{
    [Error<NotFoundException>]
    [Error<DbError>]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [UseMutationConvention]
    public async Task<User> ChangeUserAccountData(
        LoggedInContext loggedInContext,
        ChangeUserAccountDataInput input,
        LexBoxDbContext dbContext)
    {
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is null) throw new NotFoundException("User not found");
        if (loggedInContext.User.Id != input.UserId) throw new UnauthorizedAccessException();
        // below works to change email
        // minimum email = a@a.a
        // if (input.Email is not null && input.Email != ""){
        //     if (input.Email.Contains("@") == false || input.Email.Length < 3){
        //         throw new RequiredException("Email does not match requirements");
        //     }
        //     user.Email = input.Email;
        // }

        if (!String.IsNullOrEmpty(input.Name))
        {
            user.Name = input.Name;
        }

        await dbContext.SaveChangesAsync();
        return user;
    }

    [Error<NotFoundException>]
    [Error<DbError>]
    [UseMutationConvention]
    [AdminRequired]
    public async Task<User> ChangeUserAccountByAdmin(ChangeUserAccountByAdminInput input, LexBoxDbContext dbContext)
    {
        var user = await dbContext.Users.FindAsync(input.UserId);
        if (user is null) throw new NotFoundException("User not found");
        if (!String.IsNullOrEmpty(input.Name))
        {
            user.Name = input.Name;
        }

        if (!String.IsNullOrEmpty(input.Email))
        {
            user.Email = input.Email;
        }

        await dbContext.SaveChangesAsync();
        return user;
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
}

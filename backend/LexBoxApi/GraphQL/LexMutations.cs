using HotChocolate.Authorization;
using LexBoxApi.Auth;
using LexBoxApi.Models.Project;
using LexBoxApi.Services;
using LexCore.Entities;
using LexCore.Exceptions;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.GraphQL;

[MutationType]
public class LexMutations
{
}

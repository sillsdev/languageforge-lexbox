using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using LexCore;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NeinLinq;

namespace LexBoxApi.Controllers;

[ApiController]
public class LegacyProjectApiController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly ILexProxyService _lexProxyService;

    public LegacyProjectApiController(LexBoxDbContext lexBoxDbContext, ILexProxyService lexProxyService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _lexProxyService = lexProxyService;
    }

    public record ProjectsInput(string Password);

    [AllowAnonymous]
    [HttpPost("/api/user/{userName}/projects")]
    [Consumes("application/x-www-form-urlencoded")]
    [ApiExplorerSettings(IgnoreApi = true)]
    public async Task<ActionResult<LegacyApiProject[]>> ProjectsForm(string userName, [FromForm] ProjectsInput input)
    {
        return await Projects(userName, input);
    }


    [AllowAnonymous]
    [HttpPost("/api/user/{userName}/projects")]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(LegacyApiProject[]), StatusCodes.Status200OK)]
    [Consumes(MediaTypeNames.Application.Json)]
    public async Task<ActionResult<LegacyApiProject[]>> Projects(string userName, ProjectsInput? input)
    {
        var password = input?.Password ?? string.Empty;

        var user = await _lexBoxDbContext.Users.FilterByEmailOrUsername(userName)
            .Select(user => new
            {
                user.Salt,
                user.PasswordHash,
                //FLEx does not support the observer role, so if a user is an observer we need to exclude it from the list of projects
                projects = user.Projects.Where(m => m.Role != ProjectRole.Observer)
                    .Select(member => new LegacyApiProject(member.Project!.Code,
                    member.Project.Name,
                    //it seems this is largely ignored by the client as it uses the LF domain instead
                    "http://public.languagedepot.org",
                    RoleToString(member.Role)))
            })
            .ToEntityInjectable()
            .FirstOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new LegacyApiError("Unknown user"));
        }

        var validPassword = PasswordHashing.IsValidPassword(password, user.Salt, user.PasswordHash, false);
        if (!validPassword)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new LegacyApiError("Bad password"));
        }

        return user.projects.ToArray();
    }

    [InjectLambda]
    private string RoleToString(ProjectRole role) =>
        //instead of using toString which could change if we rename the enum, we only ever want to return these 3 values.
        //this needs to be ugly so that projectable will work :(
        role == ProjectRole.Manager ? "manager"
        : role == ProjectRole.Editor ? "editor"
        : "unknown";//fieldworks doesn't know about or support observers
}

public record LegacyApiProject(string Identifier, string Name, string Repository, string Role);

public record LegacyApiError(string Error);

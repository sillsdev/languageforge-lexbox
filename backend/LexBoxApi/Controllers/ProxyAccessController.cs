using System.ComponentModel.DataAnnotations;
using System.Net.Mime;
using LexCore;
using LexCore.ServiceInterfaces;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
public class ProxyAccessController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly ILexProxyService _lexProxyService;

    public ProxyAccessController(LexBoxDbContext lexBoxDbContext, ILexProxyService lexProxyService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _lexProxyService = lexProxyService;
    }

    public record ProjectsInput([Required(AllowEmptyStrings = false)] string Password);

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
    public async Task<ActionResult<LegacyApiProject[]>> Projects(string userName, ProjectsInput input)
    {
        var password = input.Password;

        var user = await _lexBoxDbContext.Users.FilterByEmail(userName)
            .Select(user => new
            {
                user.Salt,
                user.PasswordHash,
                projects = user.Projects.Select(up => new LegacyApiProject(up.Project.Code,
                    up.Project.Name,
                    "http://public.languagedepot.org",
                    up.Role.ToString().ToLower()))
            })
            .SingleOrDefaultAsync();
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
}

public record LegacyApiProject(string Identifier, string Name, string Repository, string Role);

public record LegacyApiError(string Error);

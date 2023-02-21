using System.Net;
using LexCore;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
public class ProxyAccessController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly IProxyAuthService _proxyAuthService;

    public ProxyAccessController(LexBoxDbContext lexBoxDbContext, IProxyAuthService proxyAuthService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _proxyAuthService = proxyAuthService;
    }

    [AllowAnonymous]
    [HttpPost("/api/user/{userName}/projects")]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(LegacyApiProject[]), StatusCodes.Status200OK)]
    public async Task<ActionResult<LegacyApiProject[]>> Projects(string userName, [FromForm] string password)
    {
        var user = await _lexBoxDbContext.Users.Where(user => user.Username == userName)
            .Select(user => new
            {
                user.Salt,
                user.PasswordHash,
                projects = user.Projects.Select(up => new LegacyApiProject(up.Project.Code,
                    up.Project.Name,
                    "http://public.languagedepot.org",
                    up.Role.ToString()))
            })
            .SingleOrDefaultAsync();
        if (user == null)
        {
            return NotFound(new LegacyApiError("Unknown user"));
        }

        var validPassword = PasswordHashing.RedminePasswordHash(password, user.Salt) == user.PasswordHash;
        if (!validPassword)
        {
            return StatusCode(StatusCodes.Status403Forbidden, new LegacyApiError("Bad password"));
        }

        return user.projects.ToArray();
    }
}

public record LegacyApiProject(string Identifier, string Name, string Repository, string Role);

public record LegacyApiError(string Error);
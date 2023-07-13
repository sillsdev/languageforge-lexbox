using System.Net.Mime;
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
    private readonly ILexProxyService _lexProxyService;

    public ProxyAccessController(LexBoxDbContext lexBoxDbContext, ILexProxyService lexProxyService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _lexProxyService = lexProxyService;
    }

    private record PasswordJson(string Password);
    private static readonly string PASSWORD_FORM_KEY = nameof(PasswordJson.Password).ToLower();

    [AllowAnonymous]
    [HttpPost("/api/user/{userName}/projects")]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(LegacyApiError), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(LegacyApiProject[]), StatusCodes.Status200OK)]
    [Consumes(MediaTypeNames.Application.Json, "application/x-www-form-urlencoded")]
    public async Task<ActionResult<LegacyApiProject[]>> Projects(string userName/*, [FromForm/FromBody] string password*/)
    {
        string password;
        // The legacy API supports both types (lf-classic passes JSON & chorus passes form-data)
        if (Request.HasFormContentType)
        {
            var form = await Request.ReadFormAsync();
            password = form?[PASSWORD_FORM_KEY].FirstOrDefault() ?? string.Empty;
        }
        else
        {
            var json = await Request.ReadFromJsonAsync<PasswordJson>();
            password = json?.Password ?? string.Empty;
        }

        var user = await _lexBoxDbContext.Users.Where(user => user.Username == userName)
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

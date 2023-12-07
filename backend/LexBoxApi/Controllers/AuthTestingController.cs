using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthTestingController : ControllerBase
{
    [HttpGet("requires-auth")]
    public OkObjectResult RequiresAuth()
    {
        return Ok("success: " + User.Identity?.Name ?? "Unknown");
    }

    [HttpGet("requires-admin")]
    [AdminRequired]
    public OkResult RequiresAdmin()
    {
        return Ok();
    }

    [HttpGet("requires-forgot-password")]
    [RequireAudience(LexboxAudience.ForgotPassword, true)]
    public OkResult RequiresForgotPasswordAudience()
    {
        return Ok();
    }

    [HttpGet("403")]
    [AllowAnonymous]
    public ForbidResult Forbidden()
    {
        return Forbid();
    }
}

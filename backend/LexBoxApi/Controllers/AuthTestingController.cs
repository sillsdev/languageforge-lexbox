using LexBoxApi.Auth;
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
    [RequireAudience(LexboxAudience.ForgotPassword)]
    public OkResult RequiresForgotPasswordAudience()
    {
        return Ok();
    }
}

using LexBoxApi.Auth;
using LexBoxApi.Auth.Attributes;
using LexCore;
using LexCore.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/[controller]")]
public class AuthTestingController(LoggedInContext loggedInContext) : ControllerBase
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
    [RequireScope(LexboxAuthScope.ForgotPassword, true)]
    public OkResult RequiresForgotPassword()
    {
        return Ok();
    }

    [HttpGet("requiresSendReceiveScope")]
    [RequireScope(LexboxAuthScope.SendAndReceive, true)]
    public OkResult RequiresSendReceiveScope()
    {
        return Ok();
    }

    [HttpGet("403")]
    [AllowAnonymous]
    public ForbidResult Forbidden()
    {
        return Forbid();
    }

    [HttpGet("requiresFwBetaFeatureFlag")]
    [FeatureFlagRequired(FeatureFlag.FwLiteBeta)]
    public ActionResult RequiresFwBetaFeatureFlag()
    {
        return Ok();
    }

    [HttpGet("requires-admin-and-sr-scope")]
    [AdminRequired]
    [RequireScope(LexboxAuthScope.SendAndReceive, true)]
    public ActionResult RequiresAdminAndSrScope()
    {
        return Ok();
    }

    [HttpGet("token-project-count")]
    [AllowAnonymous]
    public ActionResult<int?> TokenProjectCount()
    {
        return loggedInContext.MaybeUser?.Projects.Length;
    }
}

using Microsoft.AspNetCore.Mvc;

namespace LexBoxApi.Controllers;

#if DEBUG
[ApiController]
public class TestingController : ControllerBase
{
    [HttpGet("requires-auth")]
    public OkObjectResult RequiresAuth()
    {
        return Ok("success: " + User.Identity?.Name ?? "Unknown");
    }

    [HttpGet("requires-admin")]
    public OkResult RequiresAdmin()
    {
        throw new NotImplementedException();
    }
}
#endif
using LexCore.Entities;
using LexData;
using LexData.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
public class UserController: ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;

    public UserController(LexBoxDbContext lexBoxDbContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
    }

    [HttpGet("list")]
    public async Task<ActionResult<List<User>>> List()
    {
        return await _lexBoxDbContext.Users.Take(100).ToListAsync();
    }
}
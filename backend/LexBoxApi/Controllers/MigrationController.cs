using LexBoxApi.Auth.Attributes;
using LexBoxApi.Services;
using LexCore.Entities;
using LexData;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Controllers;

[ApiController]
[Route("/api/migrate")]
[AdminRequired]
public class MigrationController : ControllerBase
{
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly ProjectService _projectService;

    public MigrationController(LexBoxDbContext lexBoxDbContext,
        ProjectService projectService)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _projectService = projectService;
    }

    [HttpGet("migrateRepo")]
    public async Task<ActionResult> MigrateRepo(string projectCode)
    {
        await _projectService.MigrateProject(projectCode);
        return Ok();
    }

    [HttpGet("migrateStatus")]
    public async Task<ActionResult<ProjectMigrationStatus>> MigrateStatus(string projectCode)
    {
        return await _lexBoxDbContext.Projects
            .Where(p => p.Code == projectCode)
            .Select(p => p.MigrationStatus)
            .SingleAsync();
    }
}

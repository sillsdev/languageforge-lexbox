using LexBoxApi.Controllers;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;

namespace Testing.LexBoxApi;

/// <summary>
/// Unit tests for ProjectController.InitFwDataProject input validation. The 400 branches run before
/// any injected service is touched, so the controller can be built with null dependencies; only a
/// ProblemDetailsFactory (from AddControllers) is needed for Problem() to render.
/// </summary>
public class InitFwDataProjectValidationTests
{
    private static ProjectController NewController(IPermissionService? permissionService = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers(); // registers the ProblemDetailsFactory that ControllerBase.Problem() resolves
        return new ProjectController(null!, null!, null!, permissionService!, null!, null!, NullLogger<ProjectController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { RequestServices = services.BuildServiceProvider() }
            }
        };
    }

    [Fact]
    public async Task Rejects_when_no_vernacular_writing_system_is_supplied()
    {
        var result = await NewController().InitFwDataProject("myproj", wsVernacular: []);
        var problem = result.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("vernacular");
    }

    [Fact]
    public async Task Rejects_an_invalid_project_code()
    {
        var result = await NewController().InitFwDataProject("Bad Code!", wsVernacular: ["fr"]);
        var problem = result.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("Invalid project code");
    }

    /// <summary>A permission service that either passes or fails the admin assertion.</summary>
    private static IPermissionService PermissionService(bool isAdmin)
    {
        var mock = new Mock<IPermissionService>();
        if (!isAdmin) mock.Setup(p => p.AssertIsAdmin()).Throws<UnauthorizedAccessException>();
        return mock.Object;
    }

    [Fact]
    public async Task Rejects_a_project_origin_that_is_not_a_migration_status()
    {
        var result = await NewController(PermissionService(isAdmin: true))
            .InitFwDataProject("myproj", wsVernacular: ["fr"], projectOrigin: "NotAnOrigin");
        var problem = result.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("Invalid project origin");
    }

    [Fact]
    public async Task Rejects_a_project_origin_from_a_non_admin()
    {
        // The admin check must happen before the value is even parsed, and must outlive [AdminRequired],
        // which is expected to be relaxed for this endpoint later.
        var act = () => NewController(PermissionService(isAdmin: false))
            .InitFwDataProject("myproj", wsVernacular: ["fr"], projectOrigin: "Migrated");
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Ignores_an_empty_project_origin_without_requiring_admin()
    {
        // An empty value isn't "specified", so it needs no admin check; a null permission service
        // proves AssertIsAdmin() was never called. Fails later (null project service) than validation.
        var act = () => NewController().InitFwDataProject("myproj", wsVernacular: ["fr"], projectOrigin: "");
        await act.Should().ThrowAsync<NullReferenceException>();
    }

    [Theory]
    [InlineData("Migrated", ProjectMigrationStatus.Migrated)]
    [InlineData("migrating", ProjectMigrationStatus.Migrating)]
    [InlineData("PUBLICREDMINE", ProjectMigrationStatus.PublicRedmine)]
    public void Project_origin_parses_case_insensitively(string input, ProjectMigrationStatus expected)
    {
        ProjectController.TryParseProjectOrigin(input, out var origin).Should().BeTrue();
        origin.Should().Be(expected);
    }

    [Theory]
    [InlineData("NotAnOrigin")]
    [InlineData("1")] // Enum.TryParse accepts raw numbers; the API takes names only
    [InlineData("42")]
    [InlineData("Migrated,Migrating")] // ...and comma-separated lists, which OR into a defined value here
    [InlineData("")]
    public void Project_origin_rejects_values_that_are_not_named_members(string input)
    {
        ProjectController.TryParseProjectOrigin(input, out _).Should().BeFalse();
    }

    [Fact]
    public void Analysis_writing_systems_default_to_english_when_null()
    {
        ProjectController.AnalysisWritingSystemsOrDefault(null).Should().Equal("en");
    }

    [Fact]
    public void Analysis_writing_systems_default_to_english_when_empty()
    {
        ProjectController.AnalysisWritingSystemsOrDefault([]).Should().Equal("en");
    }

    [Fact]
    public void Analysis_writing_systems_are_kept_when_supplied()
    {
        ProjectController.AnalysisWritingSystemsOrDefault(["fr", "es"]).Should().Equal("fr", "es");
    }
}

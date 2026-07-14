using LexBoxApi.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace Testing.LexBoxApi;

/// <summary>
/// Unit tests for ProjectController.CreateFromTemplate input validation. The 400 branches run before
/// any injected service is touched, so the controller can be built with null dependencies; only a
/// ProblemDetailsFactory (from AddControllers) is needed for Problem() to render.
/// </summary>
public class CreateFromTemplateValidationTests
{
    private static ProjectController NewController()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddControllers(); // registers the ProblemDetailsFactory that ControllerBase.Problem() resolves
        return new ProjectController(null!, null!, null!, null!, null!, null!, NullLogger<ProjectController>.Instance)
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
        var result = await NewController().CreateFromTemplate("myproj", wsVernacular: []);
        var problem = result.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("vernacular");
    }

    [Fact]
    public async Task Rejects_an_invalid_project_code()
    {
        var result = await NewController().CreateFromTemplate("Bad Code!", wsVernacular: ["fr"]);
        var problem = result.Result.Should().BeOfType<ObjectResult>()
            .Which.Value.Should().BeOfType<ProblemDetails>().Which;
        problem.Status.Should().Be(StatusCodes.Status400BadRequest);
        problem.Detail.Should().Contain("Invalid project code");
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

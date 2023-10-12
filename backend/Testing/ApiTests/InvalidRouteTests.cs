using System.Net;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

public class InvalidRouteTests : ApiTestBase
{
    [Fact]
    public async Task ApiPathRequestsShouldBeServedByDotnetForAnonymous()
    {
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/login/not-exists");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
    [Fact]
    public async Task ApiBasePathRequestsShouldBeServedByDotnetForAuthenticated()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.GetAsync($"{BaseUrl}/api/login/not-exists");
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}

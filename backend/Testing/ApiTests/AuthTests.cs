using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using LexCore.Auth;
using Shouldly;
using Testing.Services;

namespace Testing.ApiTests;

[Trait("Category", "Integration")]
public class AuthTests : ApiTestBase
{
    [Fact]
    public async Task TestLoginAndVerifyDifferentUsers()
    {
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var managerResponse = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var manager = await managerResponse.Content.ReadFromJsonAsync<LexAuthUser>();
        manager.ShouldNotBeNull();
        manager.Email.ShouldBe("manager@test.com");

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var response = await HttpClient.SendAsync(new HttpRequestMessage(HttpMethod.Get,
                $"{BaseUrl}/api/user/currentUser"),
            HttpCompletionOption.ResponseContentRead);
        var admin = await response.Content.ReadFromJsonAsync<LexAuthUser>();
        admin.ShouldNotBeNull();
        admin.Email.ShouldBe("admin@test.com");
    }

    [Fact]
    public async Task TestGqlVerifyDifferentUsers()
    {
        var query = """query testGetMe {  me {    id    email  }}""";
        await LoginAs("manager", TestingEnvironmentVariables.DefaultPassword);
        var manager = await ExecuteGql(query);
        manager.ShouldNotBeNull();
        manager["data"]!["me"]!["email"]!.ToString().ShouldBe("manager@test.com");

        await LoginAs("admin", TestingEnvironmentVariables.DefaultPassword);
        var admin = await ExecuteGql(query);
        admin.ShouldNotBeNull();
        admin["data"]!["me"]!["email"]!.ToString().ShouldBe("admin@test.com");
    }
}

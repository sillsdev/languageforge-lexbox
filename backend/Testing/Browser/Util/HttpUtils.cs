using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using Shouldly;
using Testing.Services;

namespace Testing.Browser.Util;

public static class HttpUtils
{
    public static async Task<JsonObject> ExecuteGql(this IPage page, [StringSyntax("graphql")] string gql, bool expectGqlError = false)
    {
        var response = await page.APIRequest.PostAsync(
            $"http://{TestingEnvironmentVariables.ServerHostname}/api/graphql",
            new() { DataObject = new { query = gql } });
        response.Status.ShouldBe(200, $"code was {response.Status} ({response.StatusText})");
        var jsonResponse = await response.JsonAsync<JsonObject>();
        jsonResponse.ShouldNotBeNull("for query " + gql);
        if (!expectGqlError)
        {
            jsonResponse["errors"].ShouldBeNull();
        }
        return jsonResponse;
    }

    public static Task<JsonObject> DeleteUser(this IPage page, Guid userId)
    {
        return page.ExecuteGql($$"""
            mutation {
                deleteUserByAdminOrSelf(input: { userId: "{{userId}}" }) {
                    user {
                        id
                    }
                    errors {
                        ... on Error {
                            message
                        }
                    }
                }
            }
            """);
    }
}


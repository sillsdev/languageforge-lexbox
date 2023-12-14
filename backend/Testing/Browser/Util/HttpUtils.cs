using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using Microsoft.Playwright;
using Shouldly;
using Testing.LexCore.Utils;
using Testing.Services;

namespace Testing.Browser.Util;

public static class HttpUtils
{
    public static async Task<JsonObject> ExecuteGql(this IAPIRequestContext requestContext, [StringSyntax("graphql")] string gql, bool expectGqlError = false)
    {
        var response = await requestContext.PostAsync(
            $"{TestingEnvironmentVariables.ServerBaseUrl}/api/graphql",
            new() { DataObject = new { query = gql } });
        response.Status.ShouldBe(200, $"code was {response.Status} ({response.StatusText})");
        var jsonResponse = await response.JsonAsync<JsonObject>();
        jsonResponse.ShouldNotBeNull("for query " + gql);
        GqlUtils.ValidateGqlErrors(jsonResponse, expectGqlError);
        return jsonResponse;
    }

    public static Task<JsonObject> DeleteUser(this IAPIRequestContext requestContext, Guid userId)
    {
        return requestContext.ExecuteGql($$"""
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

    public static async Task LoginAs(this IAPIRequestContext requestContext, string user, string password)
    {
        await requestContext.PostAsync($"{TestingEnvironmentVariables.ServerBaseUrl}/api/login",
            new() { DataObject = new { password, emailOrUsername = user, preHashedPassword = false } });
    }
}

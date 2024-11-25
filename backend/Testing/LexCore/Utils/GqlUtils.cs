using System.Text.Json.Nodes;
using FluentAssertions;

namespace Testing.LexCore.Utils;

public static class GqlUtils
{
    public static void ValidateGqlErrors(JsonObject json, bool expectError = false)
    {
        if (!expectError)
        {
            json!["errors"]?.Should().BeNull();
            if (json["data"] is JsonObject data)
            {
                foreach (var (_, resultValue) in data)
                {
                    if (resultValue is JsonObject resultObject)
                        resultObject["errors"]?.Should().BeNull();
                }
            }
        }
        else
        {
            var foundError = json["errors"] is JsonArray errors && errors.Count > 0;
            if (!foundError)
            {
                if (json["data"] is JsonObject data)
                {
                    foreach (var (_, resultValue) in data)
                    {
                        if (resultValue is JsonObject resultObject)
                        {
                            foundError = resultObject["errors"] is JsonArray resultErrors && resultErrors.Count > 0;
                            if (foundError)
                                break;
                        }
                    }
                }
            }
            foundError.Should().BeTrue();
        }
    }
}

using System.Text.Json.Nodes;
using Shouldly;

namespace Testing.LexCore.Utils;

public static class GqlUtils
{
    public static void ValidateGqlErrors(JsonObject json, bool expectError = false)
    {
        if (!expectError)
        {
            json["errors"].ShouldBeNull();
            if (json["data"] is JsonObject data)
            {
                foreach (var (_, resultValue) in data)
                {
                    resultValue?["errors"].ShouldBeNull();
                }
            }
        }
    }
}

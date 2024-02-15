using System.Text.Json;
using System.Text.Json.Serialization;

namespace LexCore.Utils;

public class JsonSnakeCaseUpperStringEnumConverter : JsonStringEnumConverter
{
    public JsonSnakeCaseUpperStringEnumConverter() : base(JsonNamingPolicy.SnakeCaseUpper)
    {
    }
}

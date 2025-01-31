using System.Text.Json.Serialization;

namespace LexCore;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum FeatureFlag
{
    FwLiteBeta,
}

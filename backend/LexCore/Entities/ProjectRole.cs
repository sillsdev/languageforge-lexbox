using System.Text.Json.Serialization;

namespace LexCore.Entities;

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ProjectRole
{
    Unknown = 0,
    // Admin = 1,
    Manager = 2,
    Editor = 3,
    // Observer = 4,
}
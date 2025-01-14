using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LexBoxApi.Controllers;
using LexCore.Entities;

namespace LexBoxApi.Config;

public class FwLiteReleaseConfig
{
    public Dictionary<FwLitePlatform, FwLitePlatformConfig> Platforms { get; set; } = new();
}

public class FwLitePlatformConfig
{
    [Required]
    public required string FileNameRegex { get; init; }

    [field: AllowNull]
    public Regex FileName => field ??= new Regex(FileNameRegex);
}

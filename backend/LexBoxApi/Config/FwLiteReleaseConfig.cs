﻿using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using LexBoxApi.Controllers;
using LexCore.Entities;

namespace LexBoxApi.Config;

public class FwLiteReleaseConfig
{
    public Dictionary<FwLiteEdition, FwLiteEditionConfig> Editions { get; set; } = new();
}

public class FwLiteEditionConfig
{
    [Required]
    public required string FileNameRegex { get; init; }

    private Regex? _fileName;
    public Regex FileName => _fileName ??= new Regex(FileNameRegex);
}

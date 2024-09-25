﻿namespace MiniLcm.Models;

public record WritingSystem
{
    public required WritingSystemId Id { get; set; }
    public required string Name { get; set; }
    public required string Abbreviation { get; set; }
    public required string Font { get; set; }

    public string[] Exemplars { get; set; } = [];
    //todo probably need more stuff here, see wesay for ideas

    public static string[] LatinExemplars => Enumerable.Range('A', 'Z' - 'A' + 1).Select(c => ((char)c).ToString()).ToArray();
}

public record WritingSystems
{
    public WritingSystem[] Analysis { get; set; } = [];
    public WritingSystem[] Vernacular { get; set; } = [];
}

public enum WritingSystemType
{
    Vernacular = 0,
    Analysis = 1
}

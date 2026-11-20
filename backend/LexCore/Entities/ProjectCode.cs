using System.Text.RegularExpressions;

namespace LexCore.Entities;

public readonly partial record struct ProjectCode
{
    public ProjectCode()
    {
        throw new NotSupportedException("Default constructor is not supported.");
    }

    public ProjectCode(string value)
    {
        AssertIsSafeRepoName(value);
        Value = value;
    }

    public string Value { get; }
    public static implicit operator ProjectCode(string code) => new(code);

    public override string ToString()
    {
        return Value;
    }

    public const string DELETED_REPO_FOLDER = "_____deleted_____";
    public const string TEMP_REPO_FOLDER = "_____temp_____";
    public static readonly string[] SpecialDirectoryNames = [DELETED_REPO_FOLDER, TEMP_REPO_FOLDER];

    private static readonly HashSet<string> InvalidRepoNames =
        new([.. SpecialDirectoryNames, "api"], StringComparer.OrdinalIgnoreCase);

    private void AssertIsSafeRepoName(string name)
    {
        if (InvalidRepoNames.Contains(name))
            throw new ArgumentException($"Invalid repo name: {name}.");
        if (!IsValidCode(name))
            throw new ArgumentException($"Invalid repo name: {name}.");
    }

    /// <summary>
    /// Whether the whole string matches <see cref="Project.ProjectCodeRegex"/>. Use this rather than
    /// Regex.IsMatch with that pattern: its <c>$</c> also matches before a trailing newline, so "abcd"
    /// plus a line feed would pass, and hg URLs drop the newline, so they'd point at project "abcd".
    /// </summary>
    public static bool IsValidCode(string? code) => code is not null && ProjectCodeRegex().IsMatch(code);

    // Project.ProjectCodeRegex with \z instead of $ (see IsValidCode). That one can stay as it is for
    // [RegularExpression], which already requires the match to cover the whole string.
    [GeneratedRegex(@"^[a-z\d][a-z-\d]*\z")]
    private static partial Regex ProjectCodeRegex();
}

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
        if (!ProjectCodeRegex().IsMatch(name))
            throw new ArgumentException($"Invalid repo name: {name}.");
    }

    [GeneratedRegex(Project.ProjectCodeRegex)]
    private static partial Regex ProjectCodeRegex();
}

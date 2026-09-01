namespace LexCore.Utils;

public static class StringExtensions
{
    /// <summary>
    /// Strips line endings so user-controlled values can't forge log entries (CodeQL "log entries created from user input").
    /// </summary>
    public static string SanitizeForLog(this string value) => value.ReplaceLineEndings(string.Empty);
}

namespace LexCore.Analytics;

/// <summary>
/// Detects non-interactive/automated environments where analytics should be off (CI runners).
/// </summary>
public static class AnalyticsCiEnvironment
{
    /// <summary>
    /// True when the process is running under CI. GitHub Actions sets <c>CI</c> and <c>GITHUB_ACTIONS</c>.
    /// </summary>
    public static bool IsCiEnvironment(IReadOnlyDictionary<string, string?>? environmentVariables = null)
    {
        return IsTruthyEnv(ReadEnv(environmentVariables, "CI"))
            || IsTruthyEnv(ReadEnv(environmentVariables, "GITHUB_ACTIONS"));
    }

    public static bool IsTruthyEnv(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return value.Equals("true", StringComparison.OrdinalIgnoreCase)
            || value.Equals("1", StringComparison.OrdinalIgnoreCase)
            || value.Equals("yes", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ReadEnv(IReadOnlyDictionary<string, string?>? environmentVariables, string key)
    {
        if (environmentVariables is not null)
            return environmentVariables.TryGetValue(key, out var value) ? value : null;
        return Environment.GetEnvironmentVariable(key);
    }
}

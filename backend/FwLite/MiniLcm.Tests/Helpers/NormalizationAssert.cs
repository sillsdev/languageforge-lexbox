using System.Collections;
using System.Reflection;
using System.Text;
using FluentAssertions.Equivalency;

namespace MiniLcm.Tests.Helpers;

public static class NormalizationEquivalency
{
    /// <summary>
    /// Configure BeEquivalentTo so strings compare equal modulo NFC/NFD normalization,
    /// catching non-string fields the wrapper drops (HomographNumber, Order, etc.)
    /// that the form-only check ignores.
    /// </summary>
    public static EquivalencyOptions<T> NormalizedStrings<T>(this EquivalencyOptions<T> options)
    {
        return options
            .Using<string>(ctx =>
            {
                if (ctx.Subject is null || ctx.Expectation is null)
                {
                    ctx.Subject.Should().Be(ctx.Expectation);
                    return;
                }
                ctx.Subject.Normalize(NormalizationForm.FormD)
                    .Should().Be(ctx.Expectation.Normalize(NormalizationForm.FormD));
            })
            .WhenTypeIs<string>();
    }
}

/// <summary>
/// For verifying that every string-bearing property of an object is normalized (NFC or NFD).
/// </summary>
public static class NormalizationAssert
{
    private static readonly Dictionary<Type, HashSet<string>> SkippedProperties = new()
    {
        // WritingSystem fields are generally LDML-managed and seem to not be normalized by FieldWorks
        [typeof(WritingSystem)] =
        [
            nameof(WritingSystem.WsId),
            nameof(WritingSystem.Name),
            nameof(WritingSystem.Abbreviation),
            nameof(WritingSystem.Font),
            nameof(WritingSystem.Exemplars),
        ],
    };

    public static void AssertAllDecomposed(object? obj)
    {
        Assert(obj, "NFD", CheckNfd);
    }

    /// <summary>
    /// Strict NFC plus a non-triviality check: every string must differ from its NFD form.
    /// Catches test data, which is byte-identical in NFC and NFD (e.g. ASCII) and would
    /// silently bypass the normalizer.
    /// </summary>
    public static void AssertAllDecomposable(object? obj)
    {
        Assert(obj, "decomposable NFC", CheckDecomposableNfc);
    }

    /// <summary>
    /// For verifying the output of the write-normalization wrapper:
    /// asserts every string is NFD AND that no non-string field was dropped or mutated
    /// (BeEquivalentTo on the input, with NFC≡NFD string equivalence).
    /// Catches the field-drop regression class that pure string-form checks ignore.
    /// </summary>
    public static void AssertNormalizedToNfd<T>(T? captured, T input) where T : class
    {
        captured.Should().NotBeNull();
        AssertAllDecomposed(captured);
        captured.Should().BeEquivalentTo(input, opts => opts.NormalizedStrings());
    }

    private static void Assert(object? obj, string description, Action<string, string, List<string>> check)
    {
        if (obj is null) throw new Xunit.Sdk.XunitException("Expected object to be non-null but was null");
        var issues = FindIssues(obj, check);
        if (issues.Count == 0) return;
        throw new Xunit.Sdk.XunitException(
            $"Expected all normalizable properties to contain {description} strings, but found issues:\n" +
            string.Join("\n", issues.Select(i => "  - " + i))
        );
    }

    private static List<string> FindIssues(object obj, Action<string, string, List<string>> check)
    {
        var issues = new List<string>();
        Visit(obj, "", check, issues);
        return issues;
    }

    private static void Visit(object? obj, string path, Action<string, string, List<string>> check, List<string> issues)
    {
        switch (obj)
        {
            case null:
                return;
            case string s:
                CheckString(s, path, check, issues);
                return;
            case MultiString ms:
                if (ms.Values.Count == 0) issues.Add($"{path}: MultiString has no values (must have at least one for testing)");
                foreach (var (key, value) in ms.Values) CheckString(value, $"{path}.Values[{key}]", check, issues);
                return;
            case RichString rs:
                if (rs.Spans.Count == 0) issues.Add($"{path}: RichString has no spans (must have at least one for testing)");
                for (var i = 0; i < rs.Spans.Count; i++) CheckString(rs.Spans[i].Text, $"{path}.Spans[{i}].Text", check, issues);
                return;
            case RichMultiString rms:
                if (rms.Count == 0) issues.Add($"{path}: RichMultiString has no values (must have at least one for testing)");
                foreach (var (key, value) in rms) Visit(value, $"{path}[{key}]", check, issues);
                return;
            case IEnumerable seq:
                var i2 = 0;
                foreach (var item in seq) Visit(item, $"{path}[{i2++}]", check, issues);
                return;
            default:
                break;
        }

        VisitModelProperties(obj, path, check, issues);
    }

    private static void VisitModelProperties(object obj, string path, Action<string, string, List<string>> check, List<string> issues)
    {
        var type = obj.GetType();
        if (type.Namespace?.StartsWith("MiniLcm.Models", StringComparison.Ordinal) != true)
            throw new Xunit.Sdk.XunitException($"Unexpected type {type.FullName} at {(string.IsNullOrEmpty(path) ? "<root>" : path)}");

        var skipped = SkippedProperties.GetValueOrDefault(type);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead || skipped?.Contains(prop.Name) == true) continue;
            if (IsIgnoredType(prop.PropertyType)) continue;

            var value = prop.GetValue(obj);
            if (value is null) continue;

            var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            Visit(value, propPath, check, issues);
        }
    }

    private static bool IsIgnoredType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum ||
               underlying == typeof(Guid) || underlying == typeof(DateTime) ||
               underlying == typeof(DateTimeOffset) || underlying == typeof(decimal);
    }

    private static void CheckString(string? value, string path, Action<string, string, List<string>> check, List<string> issues)
    {
        if (string.IsNullOrEmpty(value))
        {
            issues.Add($"{path}: string is null or empty (must have a value for testing)");
            return;
        }
        check(value, path, issues);
    }

    // Per-form leaf checks. AssertAllDecomposed verifies wrapper OUTPUT is NFD; AssertAllDecomposable verifies
    // INPUT test data is NFC AND actually decomposes — content byte-identical in NFC and NFD (e.g. ASCII) would silently no-op the normalizer.
    private static void CheckNfd(string value, string path, List<string> issues)
    {
        if (!value.IsNormalized(NormalizationForm.FormD))
            issues.Add($"{path}: expected NFD but \"{value}\" is not NFD-normalized");
    }

    private static void CheckDecomposableNfc(string value, string path, List<string> issues)
    {
        if (!value.IsNormalized(NormalizationForm.FormC))
        {
            issues.Add($"{path}: expected NFC but \"{value}\" is not NFC-normalized");
            return;
        }
        if (value == value.Normalize(NormalizationForm.FormD))
            issues.Add($"{path}: \"{value}\" is trivially NFC (identical to its NFD form); use content that actually exercises normalization");
    }
}

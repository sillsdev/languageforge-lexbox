using System.Collections;
using System.Reflection;
using System.Text;

namespace MiniLcm.Tests.Helpers;

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

    public static void AssertAllNfc(object? obj, bool requireNonTrivial = false)
    {
        Assert(obj, NormalizationForm.FormC, requireNonTrivial);
    }

    public static void AssertAllNfd(object? obj, bool requireNonTrivial = false)
    {
        Assert(obj, NormalizationForm.FormD, requireNonTrivial);
    }

    public static bool IsAllNfd(object obj)
    {
        return FindIssues(obj, NormalizationForm.FormD, requireNonTrivial: false).Count == 0;
    }

    private static void Assert(object? obj, NormalizationForm form, bool requireNonTrivial)
    {
        if (obj is null) throw new Xunit.Sdk.XunitException("Expected object to be non-null but was null");
        var issues = FindIssues(obj, form, requireNonTrivial);
        if (issues.Count == 0) return;
        var name = FormName(form);
        throw new Xunit.Sdk.XunitException(
            $"Expected all normalizable properties to contain {name} strings, but found issues:\n" +
            string.Join("\n", issues.Select(i => "  - " + i))
        );
    }

    private static List<string> FindIssues(object obj, NormalizationForm form, bool requireNonTrivial)
    {
        var issues = new List<string>();
        Visit(obj, "", form, requireNonTrivial, issues);
        return issues;
    }

    private static void Visit(object? obj, string path, NormalizationForm form, bool requireNonTrivial, List<string> issues)
    {
        switch (obj)
        {
            case null:
                return;
            case string s:
                CheckString(s, path, form, requireNonTrivial, issues);
                return;
            case MultiString ms:
                if (ms.Values.Count == 0) issues.Add($"{path}: MultiString has no values (must have at least one for testing)");
                foreach (var (key, value) in ms.Values) CheckString(value, $"{path}.Values[{key}]", form, requireNonTrivial, issues);
                return;
            case RichString rs:
                if (rs.Spans.Count == 0) issues.Add($"{path}: RichString has no spans (must have at least one for testing)");
                for (var i = 0; i < rs.Spans.Count; i++) CheckString(rs.Spans[i].Text, $"{path}.Spans[{i}].Text", form, requireNonTrivial, issues);
                return;
            case RichMultiString rms:
                if (rms.Count == 0) issues.Add($"{path}: RichMultiString has no values (must have at least one for testing)");
                foreach (var (key, value) in rms) Visit(value, $"{path}[{key}]", form, requireNonTrivial, issues);
                return;
            case IEnumerable seq:
                var i2 = 0;
                foreach (var item in seq) Visit(item, $"{path}[{i2++}]", form, requireNonTrivial, issues);
                return;
            default:
                break;
        }

        VisitModelProperties(obj, path, form, requireNonTrivial, issues);
    }

    private static void VisitModelProperties(object obj, string path, NormalizationForm form, bool requireNonTrivial, List<string> issues)
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
            Visit(value, propPath, form, requireNonTrivial, issues);
        }
    }

    private static bool IsIgnoredType(Type type)
    {
        var underlying = Nullable.GetUnderlyingType(type) ?? type;
        return underlying.IsPrimitive || underlying.IsEnum ||
               underlying == typeof(Guid) || underlying == typeof(DateTime) ||
               underlying == typeof(DateTimeOffset) || underlying == typeof(decimal);
    }

    private static void CheckString(string? value, string path, NormalizationForm form, bool requireNonTrivial, List<string> issues)
    {
        if (string.IsNullOrEmpty(value))
        {
            issues.Add($"{path}: string is null or empty (must have a value for testing)");
            return;
        }
        if (!value.IsNormalized(form))
        {
            var name = FormName(form);
            issues.Add($"{path}: expected {name} but \"{value}\" is not {name}-normalized");
            return;
        }
        // Non-trivial check: the string must differ from its conversion to the OTHER form.
        // If they're equal, the string is byte-identical in both NFC and NFD (e.g., pure ASCII),
        // so it doesn't actually exercise normalization. This catches dead test coverage where
        // a field gets ASCII content and silently bypasses the normalizer.
        if (requireNonTrivial)
        {
            var otherForm = form == NormalizationForm.FormC ? NormalizationForm.FormD : NormalizationForm.FormC;
            if (value == value.Normalize(otherForm))
            {
                var name = FormName(form);
                var otherName = FormName(otherForm);
                issues.Add($"{path}: \"{value}\" is trivially {name} (identical to its {otherName} form); use content that actually exercises normalization");
            }
        }
    }

    private static string FormName(NormalizationForm form)
    {
        return form == NormalizationForm.FormC ? "NFC" : "NFD";
    }
}

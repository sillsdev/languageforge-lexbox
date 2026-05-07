using System.Collections;
using System.Reflection;
using System.Text;

namespace MiniLcm.Tests.Helpers;

/// <summary>
/// Assertion helpers for verifying NFC/NFD normalization state of objects.
/// Uses reflection to walk the object graph and check all normalizable properties.
/// On failure, error messages include the full property path (e.g., <c>Senses[0].Gloss.Values[en]</c>).
/// </summary>
public static class NormalizationAssert
{
    private static readonly HashSet<Type> NormalizableTypes =
    [
        typeof(string),
        typeof(string[]),
        typeof(MultiString),
        typeof(RichString),
        typeof(RichMultiString)
    ];

    /// <summary>
    /// Properties not normalized by the write wrapper, scoped per type. Includes both metadata
    /// (e.g. SemanticDomain.Code, RichSpan.Ws) and fields liblcm itself doesn't NFD-normalize
    /// (WritingSystem fields are LDML-managed plain strings; MorphType leading/trailing tokens are
    /// punctuation markers, not linguistic text).
    /// </summary>
    private static readonly Dictionary<Type, HashSet<string>> NotNormalizedPerType = new()
    {
        [typeof(WritingSystem)] =
        [
            nameof(WritingSystem.WsId),
            nameof(WritingSystem.Name),
            nameof(WritingSystem.Abbreviation),
            nameof(WritingSystem.Font),
            nameof(WritingSystem.Exemplars),
        ],
        [typeof(MorphTypeData)] =
        [
            nameof(MorphTypeData.LeadingToken),
            nameof(MorphTypeData.TrailingToken),
        ],
        [typeof(SemanticDomain)] = [nameof(SemanticDomain.Code)],
        [typeof(RichSpan)] = [nameof(RichSpan.Ws)],
    };

    /// <summary>
    /// Asserts that all normalizable properties in the object contain NFC strings.
    /// Throws if any property is null, empty, or contains NFD strings.
    /// </summary>
    public static void AssertAllNfc(object obj)
    {
        AssertAll(obj, expectNfc: true);
    }

    /// <summary>
    /// Asserts that all normalizable properties in the object contain NFD strings.
    /// Throws if any property contains NFC strings.
    /// </summary>
    public static void AssertAllNfd(object obj)
    {
        AssertAll(obj, expectNfc: false);
    }

    /// <summary>
    /// Returns true if all normalizable properties contain NFD strings.
    /// </summary>
    public static bool IsAllNfd(object obj)
    {
        return FindNormalizationIssues(obj, expectNfc: false).Count == 0;
    }

    private static void AssertAll(object obj, bool expectNfc)
    {
        var issues = FindNormalizationIssues(obj, expectNfc);
        if (issues.Count == 0) return;
        var form = expectNfc ? "NFC" : "NFD";
        throw new Xunit.Sdk.XunitException(
            $"Expected all normalizable properties to contain {form} strings, but found issues:\n" +
            string.Join("\n", issues.Select(i => $"  - {i}"))
        );
    }

    private static List<string> FindNormalizationIssues(object obj, bool expectNfc, string path = "")
    {
        var issues = new List<string>();
        if (obj == null) return issues;

        if (obj is string str)
        {
            CheckString(str, path, expectNfc, issues);
            return issues;
        }

        if (obj is string[] strArray)
        {
            for (var i = 0; i < strArray.Length; i++)
            {
                CheckString(strArray[i], $"{path}[{i}]", expectNfc, issues);
            }
            return issues;
        }

        if (obj is MultiString ms)
        {
            if (ms.Values.Count == 0)
            {
                issues.Add($"{path}: MultiString has no values (must have at least one for testing)");
            }
            foreach (var (key, value) in ms.Values)
            {
                CheckString(value, $"{path}.Values[{key}]", expectNfc, issues);
            }
            return issues;
        }

        if (obj is RichString rs)
        {
            if (rs.Spans.Count == 0)
            {
                issues.Add($"{path}: RichString has no spans (must have at least one for testing)");
            }
            for (var i = 0; i < rs.Spans.Count; i++)
            {
                CheckString(rs.Spans[i].Text, $"{path}.Spans[{i}].Text", expectNfc, issues);
            }
            return issues;
        }

        if (obj is RichMultiString rms)
        {
            if (rms.Count == 0)
            {
                issues.Add($"{path}: RichMultiString has no values (must have at least one for testing)");
            }
            foreach (var (key, value) in rms)
            {
                issues.AddRange(FindNormalizationIssues(value, expectNfc, $"{path}[{key}]"));
            }
            return issues;
        }

        // Top-level collection of items (e.g. a captured List<Entry>) — walk each item.
        // Property-level collections are handled below in the property loop, where IsModelType filtering applies.
        if (obj is IEnumerable topLevelCollection)
        {
            var index = 0;
            foreach (var item in topLevelCollection)
            {
                if (item != null) issues.AddRange(FindNormalizationIssues(item, expectNfc, $"{path}[{index}]"));
                index++;
            }
            return issues;
        }

        var type = obj.GetType();
        var perTypeSkip = NotNormalizedPerType.GetValueOrDefault(type);
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        {
            if (!prop.CanRead) continue;
            if (perTypeSkip is not null && perTypeSkip.Contains(prop.Name)) continue;

            var value = prop.GetValue(obj);
            if (value == null) continue;

            var propPath = string.IsNullOrEmpty(path) ? prop.Name : $"{path}.{prop.Name}";
            var propType = prop.PropertyType;

            if (NormalizableTypes.Contains(propType) ||
                propType == typeof(string[]) ||
                (propType.IsGenericType && propType.GetGenericTypeDefinition() == typeof(Nullable<>)))
            {
                issues.AddRange(FindNormalizationIssues(value, expectNfc, propPath));
            }
            else if (propType.IsPrimitive || propType.IsEnum || propType == typeof(Guid) || propType == typeof(DateTime) || propType == typeof(DateTimeOffset) || propType == typeof(decimal))
            {
                continue;
            }
            else if (value is IEnumerable enumerable)
            {
                var index = 0;
                foreach (var item in enumerable)
                {
                    if (item != null && IsModelType(item.GetType()))
                    {
                        issues.AddRange(FindNormalizationIssues(item, expectNfc, $"{propPath}[{index}]"));
                    }
                    index++;
                }
            }
            else if (IsModelType(propType))
            {
                issues.AddRange(FindNormalizationIssues(value, expectNfc, propPath));
            }
            else
            {
                throw new Xunit.Sdk.XunitException($"Unexpected property type: {propType.FullName} at {propPath}");
            }
        }

        return issues;
    }

    private static void CheckString(string? value, string path, bool expectNfc, List<string> issues)
    {
        if (string.IsNullOrEmpty(value))
        {
            issues.Add($"{path}: string is null or empty (must have a value for testing)");
            return;
        }

        var expected = expectNfc ? NormalizationForm.FormC : NormalizationForm.FormD;
        if (!value.IsNormalized(expected))
        {
            var formName = expectNfc ? "NFC" : "NFD";
            issues.Add($"{path}: expected {formName} but \"{value}\" is not {formName}-normalized");
        }
    }

    private static bool IsModelType(Type type)
    {
        return
            type.Namespace?.StartsWith("MiniLcm.Models") == true ||
            type == typeof(Entry) ||
            type == typeof(Sense) ||
            type == typeof(ExampleSentence) ||
            type == typeof(Translation) ||
            type == typeof(WritingSystem) ||
            type == typeof(PartOfSpeech) ||
            type == typeof(SemanticDomain) ||
            type == typeof(ComplexFormType) ||
            type == typeof(MorphTypeData) ||
            type == typeof(Publication) ||
            type == typeof(ComplexFormComponent);
    }
}

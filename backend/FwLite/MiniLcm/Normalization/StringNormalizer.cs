using System.Text;
using MiniLcm.Models;
using SystemTextJsonPatch;

namespace MiniLcm.Normalization;

/// <summary>
/// Helper class for normalizing strings to NFD (Unicode Normalization Form D - Canonical Decomposition)
/// </summary>
public static class StringNormalizer
{
    public const NormalizationForm Form = NormalizationForm.FormD;

    /// <summary>
    /// Normalizes a string to NFD. Returns null if input is null.
    /// </summary>
    public static string? Normalize(string? value)
    {
        return value?.Normalize(Form);
    }

    /// <summary>
    /// Normalizes all values in a MultiString to NFD
    /// </summary>
    public static MultiString Normalize(MultiString multiString)
    {
        var normalized = new MultiString(multiString.Values.Count);
        foreach (var (key, value) in multiString.Values)
        {
            // Preserve all keys, even if the value is empty or null
            normalized.Values[key] = string.IsNullOrEmpty(value) ? value : value.Normalize(Form);
        }
        return normalized;
    }

    /// <summary>
    /// Normalizes all text spans in a RichString to NFD
    /// </summary>
    public static RichString? Normalize(RichString? richString)
    {
        if (richString is null) return null;
        
        var normalizedSpans = richString.Spans.Select(span => 
            span with { Text = span.Text.Normalize(Form) }
        ).ToList();
        
        return new RichString(normalizedSpans);
    }

    /// <summary>
    /// Normalizes all values in a RichMultiString to NFD
    /// </summary>
    public static RichMultiString Normalize(RichMultiString richMultiString)
    {
        var normalized = new RichMultiString(richMultiString.Count);
        foreach (var (key, value) in richMultiString)
        {
            var normalizedRichString = Normalize(value);
            if (normalizedRichString is not null)
            {
                normalized[key] = normalizedRichString;
            }
        }
        return normalized;
    }

    /// <summary>
    /// Normalizes an array of strings to NFD
    /// </summary>
    public static string[] Normalize(string[] values)
    {
        return values.Select(v => v?.Normalize(Form) ?? string.Empty).ToArray();
    }

    /// <summary>
    /// Handles JsonPatchDocument normalization.
    /// Note: The patch itself is returned as-is because:
    /// 1. JsonPatch operations contain serialized values that are complex to normalize
    /// 2. The primary normalization occurs in the overridden Update* methods that accept
    ///    concrete objects (UpdateWritingSystem, UpdateEntry, etc.)
    /// 3. The objects being patched should already be normalized from creation
    /// 4. For critical patch-based updates, callers should use the object-based Update methods
    /// 
    /// If deep patch normalization is needed in the future, it would require:
    /// - Inspecting operation paths to identify string fields
    /// - Deserializing and re-serializing operation values
    /// - Handling nested object structures in patch paths
    /// </summary>
    public static JsonPatchDocument<T> NormalizePatch<T>(JsonPatchDocument<T> patch) where T : class
    {
        // Return patch as-is. See method summary for rationale.
        // Most updates use object-based methods which are fully normalized.
        return patch;
    }
}

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
            if (!string.IsNullOrEmpty(value))
            {
                normalized.Values[key] = value.Normalize(Form);
            }
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
        return values.Select(v => v.Normalize(Form)).ToArray();
    }

    /// <summary>
    /// Normalizes string values in a JsonPatchDocument by applying normalization to the underlying operations.
    /// Note: This is a simplified approach that doesn't deeply inspect operation values.
    /// Most normalization happens at the object level before creating patches.
    /// </summary>
    public static JsonPatchDocument<T> NormalizePatch<T>(JsonPatchDocument<T> patch) where T : class
    {
        // For now, we return the patch as-is since the objects being patched
        // should already be normalized before the patch is created.
        // If we need deeper normalization of patch values, we would need to
        // access the private/internal operation fields which isn't straightforward.
        // The primary normalization happens in the Normalize* methods for each type.
        return patch;
    }
}

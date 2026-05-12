using System.Diagnostics.CodeAnalysis;
using System.Text;
using MiniLcm.Models;

namespace MiniLcm.Normalization;

public static class StringNormalizer
{
    public const NormalizationForm Form = NormalizationForm.FormD;

    [return: NotNullIfNotNull(nameof(value))]
    public static string? Normalize(string? value)
    {
        return value?.Normalize(Form);
    }

    public static MultiString Normalize(MultiString multiString)
    {
        var normalized = new MultiString(multiString.Values.Count);
        foreach (var (key, value) in multiString.Values)
        {
            normalized.Values[key] = string.IsNullOrEmpty(value) ? value : value.Normalize(Form);
        }
        return normalized;
    }

    [return: NotNullIfNotNull(nameof(richString))]
    public static RichString? Normalize(RichString? richString)
    {
        if (richString is null) return null;
        return new RichString([.. richString.Spans.Select(span => span with
        {
            Text = span.Text.Normalize(Form)
        })]);
    }

    public static RichMultiString Normalize(RichMultiString richMultiString)
    {
        var normalized = new RichMultiString(richMultiString.Count);
        foreach (var (key, value) in richMultiString)
        {
            normalized[key] = Normalize(value);
        }
        return normalized;
    }
}

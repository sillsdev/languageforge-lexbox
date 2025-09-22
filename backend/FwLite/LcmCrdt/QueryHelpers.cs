using System.Globalization;

namespace LcmCrdt;

public static class QueryHelpers
{
    public static void Finalize(this Entry entry, IComparer<ComplexFormComponent> complexFormComparer)
    {
        entry.Senses.ApplySortOrder();
        entry.Components.ApplySortOrder();
        entry.ComplexForms.Sort(complexFormComparer);
        foreach (var sense in entry.Senses)
        {
            sense.Finalize();
        }
    }

    public static void Finalize(this Sense sense)
    {
        sense.ExampleSentences.ApplySortOrder();
        foreach (var exampleSentence in sense.ExampleSentences)
        {
            exampleSentence.Finalize();
        }
    }

    public static void Finalize(this ExampleSentence exampleSentence)
    {
        var firstTranslation = exampleSentence.Translations.FirstOrDefault();
#pragma warning disable CS0618 // Type or member is obsolete
        if (firstTranslation?.Id == Translation.MissingTranslationId)
        {
            firstTranslation.Id = exampleSentence.DefaultFirstTranslationId;
        }
#pragma warning restore CS0618 // Type or member is obsolete
    }

    private static void ApplySortOrder<T>(this List<T> items) where T : IOrderable
    {
        items.Sort((a, b) =>
        {
            var result = a.Order.CompareTo(b.Order);
            if (result != 0) return result;
            return a.Id.CompareTo(b.Id);
        });
    }

    public static IComparer<ComplexFormComponent> AsComplexFormComparer(this CompareInfo compareInfo)
    {
        return Comparer<ComplexFormComponent>.Create((a, b) =>
        {
            var result = compareInfo.Compare(a.ComplexFormHeadword, b.ComplexFormHeadword, CompareOptions.IgnoreCase);
            if (result != 0) return result;
            return a.ComplexFormEntryId.CompareTo(b.ComplexFormEntryId);
        });
    }
}

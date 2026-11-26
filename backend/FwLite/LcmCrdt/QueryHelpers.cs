using System.Globalization;

namespace LcmCrdt;

public static class QueryHelpers
{
    public static void Finalize(this Entry entry,
        IComparer<string> headwordComparer)
    {
        entry.Senses.ApplySortOrder();
        entry.Components.ApplySortOrder();
        entry.SortComplexForms(headwordComparer);
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
        if (Translation.IsMissingTranslationId(firstTranslation?.Id))
        {
            firstTranslation.Id = exampleSentence.DefaultFirstTranslationId;
        }
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
}

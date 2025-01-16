using System.Globalization;

namespace LcmCrdt;

public static class QueryHelpers
{
    public static void ApplySortOrder(this Entry entry, CompareInfo sortCompareInfo)
    {
        entry.Senses.ApplySortOrder();
        entry.Components.ApplySortOrder();
        entry.ComplexForms.Sort((a, b) => ComplexFormsComparison(a, b, sortCompareInfo));
        foreach (var sense in entry.Senses)
        {
            sense.ApplySortOrder();
        }
    }

    public static void ApplySortOrder(this Sense sense)
    {
        sense.ExampleSentences.ApplySortOrder();
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

    private static int ComplexFormsComparison(ComplexFormComponent a, ComplexFormComponent b, CompareInfo compareInfo)
    {
        var result = compareInfo.Compare(a.ComplexFormHeadword, b.ComplexFormHeadword, CompareOptions.IgnoreCase);
        if (result != 0) return result;
        return a.ComplexFormEntryId.CompareTo(b.ComplexFormEntryId);
    }
}

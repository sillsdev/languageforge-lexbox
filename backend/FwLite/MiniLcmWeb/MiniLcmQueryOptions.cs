using System.ComponentModel;
using Microsoft.AspNetCore.Mvc;
using MiniLcm;
using MiniLcm.Filtering;

namespace MiniLcmWeb;

internal class MiniLcmQueryOptions
{
    public QueryOptions ToQueryOptions()
    {
        var sortField = SortField ?? SortOptions.Default.Field;
        return new QueryOptions(new SortOptions(sortField,
                SortWritingSystem ?? SortOptions.Default.WritingSystem,
                Ascending ?? SortOptions.Default.Ascending),
            ToExemplarOptions(),
            Count ?? QueryOptions.Default.Count,
            Offset ?? QueryOptions.Default.Offset,
            ToEntryFilter(GridifyFilter));
    }

    public IndexQueryOptions ToIndexQueryOptions()
    {
        var sortField = SortField ?? SortOptions.Default.Field;
        return new IndexQueryOptions(new SortOptions(sortField,
                SortWritingSystem ?? SortOptions.Default.WritingSystem,
                Ascending ?? SortOptions.Default.Ascending),
            ToExemplarOptions(),
            ToEntryFilter(GridifyFilter));
    }

    private ExemplarOptions? ToExemplarOptions() =>
        string.IsNullOrEmpty(ExemplarValue) || ExemplarWritingSystem is null
            ? null
            : new(ExemplarValue, ExemplarWritingSystem);

    internal static EntryFilter? ToEntryFilter(string? gridifyFilter) =>
        string.IsNullOrEmpty(gridifyFilter) ? null : new EntryFilter { GridifyFilter = gridifyFilter };

    public SortField? SortField { get; set; } = SortOptions.Default.Field;

    [DefaultValue(SortOptions.DefaultWritingSystem)]
    public string? SortWritingSystem { get; set; } = SortOptions.Default.WritingSystem;

    [FromQuery]
    [DefaultValue(true)]
    public bool? Ascending { get; set; }

    [FromQuery]
    public string? ExemplarValue { get; set; }

    public string? ExemplarWritingSystem { get; set; }

    [FromQuery]
    [DefaultValue(QueryOptions.DefaultCount)]
    public int? Count { get; set; }

    [FromQuery]
    public int? Offset { get; set; }
    [FromQuery]
    public string? GridifyFilter { get; set; }
}

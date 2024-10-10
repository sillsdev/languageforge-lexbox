using LcmCrdt.Data;
using MiniLcm.Models;

namespace LcmCrdt.Tests.Data;

public class FilteringTests
{
    private readonly List<Entry> _entries;

    public FilteringTests()
    {
        _entries =
        [
            new Entry { LexemeForm = { { "en", "123" } }, },
            new Entry { LexemeForm = { { "en", "456" } }, }
        ];
    }

    [Theory]
    [InlineData("1")]
    [InlineData("9")]
    [InlineData("4")]
    public void WhereExemplar_CompiledFilter_ShouldReturnSameResults(string exemplar)
    {
        WritingSystemId ws = "en";

        var expected = _entries.AsQueryable().WhereExemplar(ws, exemplar).ToArray();
        var actual = _entries.Where(Filtering.CompiledFilter(null, ws, exemplar)).ToArray();

        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("2")]
    [InlineData("5")]
    [InlineData("9")]
    public void SearchFilter_CompiledFilter_ShouldReturnSameResults(string query)
    {
        var expected = _entries.AsQueryable().Where(Filtering.SearchFilter(query)).ToList();

        var actual = _entries.Where(Filtering.CompiledFilter(query, "en", null)).ToList();

        actual.Should().BeEquivalentTo(expected);
    }

    [Theory]
    [InlineData("1", "12")]
    [InlineData("9", "9")]
    [InlineData("4", "45")]
    [InlineData("1", "45")]
    public void CombinedFilter_CompiledFilter_ShouldReturnSameResults(string exemplar, string query)
    {
        WritingSystemId ws = "en";

        var expected = _entries.AsQueryable()
            .WhereExemplar(ws, exemplar)
            .Where(Filtering.SearchFilter(query))
            .ToList();

        var actual = _entries.Where(Filtering.CompiledFilter(query, ws, exemplar)).ToList();

        actual.Should().BeEquivalentTo(expected);
    }
}

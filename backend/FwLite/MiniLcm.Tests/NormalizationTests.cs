using MiniLcm.Normalization;
using Moq;

namespace MiniLcm.Tests;

public class NormalizationTests
{
    public IMiniLcmApi MockApi { get; init; }
    public IMiniLcmApi NormalizingApi { get; init; }

    public const string NFCString = "na\u00efve"; // "naïve" with U+00EF LATIN SMALL LETTER I WITH DIAERESIS
    public const string NFDString = "na\u0069\u0308ve"; // "naïve" with U+0069 LATIN SMALL LETTER I + U+0308 COMBINING DIAERESIS

    public FilterQueryOptions NFCOptions = new()
    {
        Exemplar = new ExemplarOptions(NFCString, "en"),
        Filter = new Filtering.EntryFilter { GridifyFilter = NFCString },
    };

    public QueryOptions NFCQueryOptions = new()
    {
        Exemplar = new ExemplarOptions(NFCString, "en"),
        Filter = new Filtering.EntryFilter { GridifyFilter = NFCString },
    };

    public FilterQueryOptions NFDOptions = new()
    {
        Exemplar = new ExemplarOptions(NFDString, "en"),
        Filter = new Filtering.EntryFilter { GridifyFilter = NFDString },
    };

    public NormalizationTests()
    {
        MockApi = Mock.Of<IMiniLcmApi>();
        // Mock.Get(MockApi).Setup(api => api.SearchEntries(It.IsAny<string>(), null)).Returns(new List<Entry>().ToAsyncEnumerable());
        var factory = new MiniLcmApiStringNormalizationWrapperFactory();
        NormalizingApi = factory.Create(MockApi);
    }

    [Fact]
    public void SearchEntriesIsNormalized()
    {
        NormalizingApi.Should().BeOfType<MiniLcmApiStringNormalizationWrapper>();
        var results = NormalizingApi.SearchEntries(NFCString, null);
        Mock.Get(MockApi).Verify(api => api.SearchEntries(NFDString, null));
    }

    [Fact]
    public void SearchEntriesWithQueryOptionsAreNormalized()
    {
        NormalizingApi.Should().BeOfType<MiniLcmApiStringNormalizationWrapper>();
        var results = NormalizingApi.SearchEntries(NFCString, NFCQueryOptions);
        Mock.Get(MockApi).Verify(api => api.SearchEntries(NFDString, It.Is<QueryOptions>(
            opt => opt.Exemplar!.Value == NFDOptions.Exemplar!.Value &&
                   opt.Filter!.GridifyFilter == NFDOptions.Filter!.GridifyFilter)));
    }

    [Fact]
    public void CountEntriesIsNormalized()
    {
        NormalizingApi.Should().BeOfType<MiniLcmApiStringNormalizationWrapper>();
        var results = NormalizingApi.CountEntries(NFCString, null);
        Mock.Get(MockApi).Verify(api => api.CountEntries(NFDString, null));
    }

    [Fact]
    public void CountEntriesWithFilterQueryOptionsIsNormalized()
    {
        NormalizingApi.Should().BeOfType<MiniLcmApiStringNormalizationWrapper>();
        var results = NormalizingApi.CountEntries(NFCString, NFCOptions);
        Mock.Get(MockApi).Verify(api => api.CountEntries(NFDString, It.Is<FilterQueryOptions>(
            opt => opt.Exemplar!.Value == NFDOptions.Exemplar!.Value &&
                   opt.Filter!.GridifyFilter == NFDOptions.Filter!.GridifyFilter)));
    }

    [Fact]
    public void GetEntriesIsNormalized()
    {
        NormalizingApi.Should().BeOfType<MiniLcmApiStringNormalizationWrapper>();
        var results = NormalizingApi.GetEntries(NFCQueryOptions);
        Mock.Get(MockApi).Verify(api => api.GetEntries(It.Is<QueryOptions>(
            opt => opt.Exemplar!.Value == NFDOptions.Exemplar!.Value &&
                   opt.Filter!.GridifyFilter == NFDOptions.Filter!.GridifyFilter)));
    }
}

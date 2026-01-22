using System.Text;
using MiniLcm.Models;
using MiniLcm.Wrappers;

namespace MiniLcm.Normalization;

public class MiniLcmApiStringNormalizationWrapperFactory() : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiStringNormalizationWrapper(api);
    }
}

public partial class MiniLcmApiStringNormalizationWrapper(
    IMiniLcmApi api) : IMiniLcmApi
{
    public const NormalizationForm Form = NormalizationForm.FormD;

    [BeaKona.AutoInterface(IncludeBaseInterfaces = true, MemberMatch = BeaKona.MemberMatchTypes.Any)]
    private readonly IMiniLcmApi _api = api;

    // ********** Overrides go here **********

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return _api.SearchEntries(query.Normalize(Form), options?.Normalized(Form));
    }

    public Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        return _api.CountEntries(query?.Normalize(Form), options?.Normalized(Form));
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return _api.GetEntries(options?.Normalized(Form));
    }

    public Task<int> GetEntryIndex(Guid entryId, string? query = null, IndexQueryOptions? options = null)
    {
        return _api.GetEntryIndex(entryId, query?.Normalize(Form), options?.Normalized(Form));
    }

    void IDisposable.Dispose()
    {
    }
}

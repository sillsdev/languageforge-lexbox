using System.Text;
using MiniLcm;
using MiniLcm.Models;
using MiniLcm.SyncHelpers;
using MiniLcm.Wrappers;

namespace MiniLcm.Validators;

public class MiniLcmApiStringNormalizationWrapperFactory(NormalizationForm normalization = NormalizationForm.FormD) : IMiniLcmWrapperFactory
{
    public IMiniLcmApi Create(IMiniLcmApi api, IProjectIdentifier _unused) => Create(api);

    public IMiniLcmApi Create(IMiniLcmApi api)
    {
        return new MiniLcmApiStringNormalizationWrapper(api, normalization);
    }
}

public partial class MiniLcmApiStringNormalizationWrapper(
    IMiniLcmApi api,
    NormalizationForm form) : IMiniLcmApi
{
    [BeaKona.AutoInterface(IncludeBaseInterfaces = true)]
    private readonly IMiniLcmApi _api = api;

    // ********** Overrides go here **********

    public IAsyncEnumerable<Entry> SearchEntries(string query, QueryOptions? options = null)
    {
        return _api.SearchEntries(query.Normalize(form), options?.Normalized(form));
    }

    public Task<int> CountEntries(string? query = null, FilterQueryOptions? options = null)
    {
        return _api.CountEntries(query?.Normalize(form), options?.Normalized(form));
    }

    public IAsyncEnumerable<Entry> GetEntries(QueryOptions? options = null)
    {
        return _api.GetEntries(options?.Normalized(form));
    }

    void IDisposable.Dispose()
    {
    }
}

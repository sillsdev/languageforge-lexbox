using MiniLcm.Normalization;
using MiniLcm.Tests.AutoFakerHelpers;
using MiniLcm.Wrappers;
using Soenneker.Utils.AutoBogus;

namespace MiniLcm.Tests;

public abstract class MiniLcmTestBase : IAsyncLifetime
{
    protected static readonly AutoFaker AutoFaker = new(AutoFakerDefault.MakeConfig(["en"], 5));
    protected IMiniLcmApi Api = null!;
    protected IMiniLcmApi BaseApi = null!;

    protected abstract Task<IMiniLcmApi> NewApi();

    public virtual async Task InitializeAsync()
    {
        BaseApi = await NewApi();
        BaseApi.Should().NotBeNull();
        // Apply both normalization wrappers:
        // - Write wrapper normalizes user input to NFD on create/update
        // - String wrapper normalizes search queries and filter options to NFD
        Api = BaseApi.WrapWith([
            new MiniLcmWriteApiNormalizationWrapperFactory(),
            new MiniLcmApiStringNormalizationWrapperFactory()
        ], null!);
        Api.Should().NotBeNull();
    }

    public virtual async Task DisposeAsync()
    {
        if (Api is IAsyncDisposable asyncDisposable)
        {
            await asyncDisposable.DisposeAsync();
        }
        else if (Api is IDisposable disposable)
        {
            disposable.Dispose();
        }
    }
}

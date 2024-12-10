using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;
using Soenneker.Utils.AutoBogus.Config;

namespace MiniLcm.Tests;

public abstract class MiniLcmTestBase : IAsyncLifetime
{
    protected static readonly AutoFaker AutoFaker = new(new AutoFakerConfig()
    {
        RepeatCount = 5,
        Overrides =
        [
            new MultiStringOverride(["en"]),
            new ObjectWithIdOverride(),
            new OrderableOverride(),
        ]
    });
    protected IMiniLcmApi Api = null!;

    protected abstract Task<IMiniLcmApi> NewApi();

    public virtual async Task InitializeAsync()
    {
        Api = await NewApi();
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

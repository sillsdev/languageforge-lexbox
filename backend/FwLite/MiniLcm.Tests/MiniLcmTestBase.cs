using MiniLcm.Tests.AutoFakerHelpers;
using Soenneker.Utils.AutoBogus;

namespace MiniLcm.Tests;

public abstract class MiniLcmTestBase : IAsyncLifetime
{

    protected static readonly AutoFaker AutoFaker = new(builder =>
        builder.WithOverride(new MultiStringOverride(["en"]))
            .WithOverride(new ObjectWithIdOverride())
    );
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

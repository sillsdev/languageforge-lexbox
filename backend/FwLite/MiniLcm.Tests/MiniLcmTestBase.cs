namespace MiniLcm.Tests;

public abstract class MiniLcmTestBase : IAsyncLifetime
{

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

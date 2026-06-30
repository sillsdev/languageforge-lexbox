using MiniLcm.Tests.AutoFakerHelpers;
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
        Api = TestMiniLcmWrappers.CreateUserFacingWrappers().Apply(BaseApi, null!);
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

    /// <summary>
    /// Returns the project's single main publication, creating one only if the project doesn't already ship with it
    /// (FwData's protected "Main Dictionary" — creating a second would be rejected).
    /// </summary>
    protected async Task<Publication> GetOrCreateMainPublication()
    {
        var existing = (await Api.GetPublications().ToArrayAsync()).FirstOrDefault(p => p.IsMain);
        return existing ?? await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });
    }
}

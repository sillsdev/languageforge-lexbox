using FwLiteShared.Services;
using FwLiteShared.LocalSync;
using Microsoft.Extensions.DependencyInjection;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.LocalSync;

/// <summary>
/// A device that has never signed into Lexbox has no Lexbox user id, but offline (local) sync still needs
/// its commits and user comments attributed, and still needs comments to work at all.
/// </summary>
public class LocalAuthorTests : IAsyncLifetime
{
    private readonly MiniLcmApiFixture _fixture = MiniLcmApiFixture.Create(configureServices: services =>
    {
        services.AddSingleton<IPreferencesService, InMemoryPreferencesService>();
        services.AddSingleton<ILocalAuthorProvider, PreferencesLocalAuthorProvider>();
    });

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        // a role that may edit, but no Lexbox user
        await _fixture.GetService<CurrentProjectService>().UpdateUserRole(UserProjectRole.Editor);
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    private Task<CommentThread> CreateThread(string text = "First") =>
        _fixture.Api.CreateCommentThread(
            new CommentThread { Id = Guid.NewGuid(), SubjectType = SubjectType.Entry, SubjectId = Guid.NewGuid() },
            new UserComment { Id = Guid.NewGuid(), Text = text });

    [Fact]
    public async Task WithoutALexboxUser_CommentThreadsAreAuthoredByTheAnonymousLocalAuthor()
    {
        var first = await CreateThread();
        var second = await CreateThread();

        first.AuthorId.Should().StartWith("anonymous:");
        second.AuthorId.Should().Be(first.AuthorId, "the local author id is stable for this device");
        first.AuthorName.Should().Be(Environment.MachineName);
    }

    [Fact]
    public async Task WithoutALexboxUser_TheLocalAuthorCanEditTheirOwnComment()
    {
        var thread = await CreateThread("before");
        var comment = (await _fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Single();

        var edited = await _fixture.Api.EditUserComment(comment.Id, "after");

        edited.Text.Should().Be("after");
    }

    [Fact]
    public async Task AfterSigningIntoLexbox_CommentsWrittenAnonymouslyOnThisDeviceStayEditable()
    {
        var thread = await CreateThread("before");
        var comment = (await _fixture.Api.GetUserComments(thread.Id).ToArrayAsync()).Single();
        await _fixture.GetService<CurrentProjectService>().UpdateLastUser("Real Person", "lexbox-user-1");

        var edited = await _fixture.Api.EditUserComment(comment.Id, "after");

        edited.Text.Should().Be("after");
    }

    [Fact]
    public async Task WithoutALexboxUser_CommitsAreAttributedToTheLocalAuthor()
    {
        var thread = await CreateThread();
        await _fixture.Api.CreateEntry(new Entry { LexemeForm = { ["en"] = "apple" } });

        var commits = (await _fixture.DataModel.GetChanges(new SyncState([]))).MissingFromClient;
        var latest = commits.MaxBy(c => c.HybridDateTime)!;

        latest.Metadata.AuthorId.Should().Be(thread.AuthorId);
        latest.Metadata.AuthorName.Should().Be(Environment.MachineName);
    }

    [Fact]
    public async Task WithAKnownLexboxUser_TheLexboxAccountIsUsed()
    {
        await _fixture.GetService<CurrentProjectService>().UpdateLastUser("Real Person", "lexbox-user-1");

        var thread = await CreateThread();

        thread.AuthorId.Should().Be("lexbox-user-1");
        thread.AuthorName.Should().Be("Real Person");
    }
}

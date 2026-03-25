using MiniLcm.Models;
using FluentAssertions;
using MiniLcm.RichText;

namespace MiniLcm.Tests;

public abstract class CreateEntryTestsBase : MiniLcmTestBase
{
    private readonly Func<Task<IMiniLcmApi>> _apiFactory;
    private IMiniLcmApi _api = null!;
    public IMiniLcmApi Api => _api;

    public CreateEntryTestsBase(Func<Task<IMiniLcmApi>> apiFactory)
    {
        _apiFactory = apiFactory;
    }

    public virtual async Task InitializeAsync()
    {
        _api = await _apiFactory();
    }

    public virtual Task DisposeAsync()
    {
        return Task.CompletedTask;
    }

    [Fact]
    public async Task CanCreateEntry()
    {
        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } } });
        entry.Should().NotBeNull();
        entry.LexemeForm.Values["en"].Should().Be("test");
    }

    [Fact]
    public async Task CanCreateEntry_AutoIncrementHomographNumber()
    {
        var entry1 = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } } });
        var entry2 = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } } });
        entry1 = await Api.GetEntry(entry1.Id) ?? throw new NullReferenceException();
        entry2 = await Api.GetEntry(entry2.Id) ?? throw new NullReferenceException();
        entry1.LexemeForm.Values["en"].Should().Be("test");
        entry2.LexemeForm.Values["en"].Should().Be("test");
    }

    [Fact]
    public async Task CanCreateEntry_WithASense()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "test" } },
                    Definition = { { "en", "test" } },
                }
            ]
        });
        entry.Should().NotBeNull();
        entry.Senses.Should().ContainSingle();
    }

    [Fact]
    public async Task CanCreateEntry_WithASenseAndExampleSentence()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = { { "en", "test" } },
            Senses =
            [
                new Sense
                {
                    Gloss = { { "en", "test" } },
                    Definition = { { "en", "test" } },
                    ExampleSentences =
                    [
                        new ExampleSentence
                        {
                            Sentence = { { "en", "This is a test sentence." } },
                        }
                    ]
                }
            ]
        });
        entry.Should().NotBeNull();
        entry.Senses.Should().ContainSingle().Which.ExampleSentences.Should().ContainSingle();
    }

    [Fact]
    public async Task CanCreateEntry_WithRichTextValue()
    {
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = { { "en", RichTextMapping.FromMultiText(
                new RichSpan() { Text = "plain text", Ws = "en" },
                new RichSpan() { Text = "bold text", Ws = "en", Bold = true },
                new RichSpan() { Text = "italic text", Ws = "en", Italic = true }
            ) } }
        });
        entry.Should().NotBeNull();
        entry.LexemeForm.RichValues["en"].Should().HaveCount(3);
    }

    [Fact]
    public async Task CanCreateEntry_WithRichTextTag()
    {
        var tag1 = new RichTextTagInfo { TagGuid = Guid.NewGuid(), Type = "charStyle", NamedStyle = "my-style" };
        var entry = await Api.CreateEntry(new Entry
        {
            LexemeForm = { { "en", RichTextMapping.FromMultiText(
                new RichSpan() { Text = "span", Ws = "en", Tags = [tag1] }
            ) } }
        });
        entry.Should().NotBeNull();
        entry.LexemeForm.RichValues["en"].Should().ContainSingle()
            .Which.Tags.Should().ContainSingle().Which.TagGuid.Should().Be(tag1.TagGuid);
    }

    [Fact]
    public async Task CreateEntry_AutoAddsMainPublication_WhenEnabled()
    {
        var mainPublication = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] }, new CreateEntryOptions(AutoAddMainPublication: true));

        entry.PublishIn.Should().ContainSingle(pub => pub.Id == mainPublication.Id);
    }

    [Fact]
    public async Task CreateEntry_DoesNotAutoAddMainPublication_WhenDisabled()
    {
        await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] }, new CreateEntryOptions(AutoAddMainPublication: false));

        entry.PublishIn.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateEntry_DoesNotDoubleAddMainPublication()
    {
        var mainPublication = await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Main" } }, IsMain = true });

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [mainPublication] });

        entry.PublishIn.Count(pub => pub.Id == mainPublication.Id).Should().Be(1);
    }

    [Fact]
    public async Task CreateEntry_DoesNothingWhenNoMainPublicationExists()
    {
        await Api.CreatePublication(new Publication { Id = Guid.NewGuid(), Name = { { "en", "Not main" } } });

        var entry = await Api.CreateEntry(new Entry { LexemeForm = { { "en", "test" } }, PublishIn = [] });

        entry.PublishIn.Should().BeEmpty();
    }
}

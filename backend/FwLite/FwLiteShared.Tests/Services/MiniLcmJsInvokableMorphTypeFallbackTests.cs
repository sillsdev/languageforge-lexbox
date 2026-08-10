using FwLiteShared.Services;
using MiniLcm.Models;

namespace FwLiteShared.Tests.Services;

public class MiniLcmJsInvokableMorphTypeFallbackTests
{
    [Fact]
    public void ApplyEmptyMorphTypeFallback_WhenEmpty_ReturnsCanonicalCopies()
    {
        var result = MiniLcmJsInvokable.ApplyEmptyMorphTypeFallback([]);

        result.Should().BeEquivalentTo(CanonicalMorphTypes.All.Values);
        foreach (var returned in result)
        {
            ReferenceEquals(returned, CanonicalMorphTypes.All[returned.Kind]).Should().BeFalse(
                "fallback must return copies so callers cannot mutate frozen canonicals");
        }
    }

    [Fact]
    public void ApplyEmptyMorphTypeFallback_WhenNonEmpty_PassesThrough()
    {
        var existing = new MorphType
        {
            Id = Guid.NewGuid(),
            Kind = MorphTypeKind.Stem,
            Name = new MultiString { { "en", "custom" } },
            Abbreviation = new MultiString { { "en", "c" } },
            Description = new RichMultiString(),
        };

        var result = MiniLcmJsInvokable.ApplyEmptyMorphTypeFallback([existing]);

        result.Should().ContainSingle().Which.Should().BeSameAs(existing);
    }
}

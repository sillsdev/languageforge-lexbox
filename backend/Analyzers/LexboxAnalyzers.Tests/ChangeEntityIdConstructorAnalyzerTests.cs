using System.Threading.Tasks;
using LexboxAnalyzers.Rules;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    LexboxAnalyzers.Rules.ChangeEntityIdConstructorAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace LexboxAnalyzers.Tests;

public class ChangeEntityIdConstructorAnalyzerTests
{
    // Minimal in-source stand-in for the Harmony change hierarchy so the tests don't need the real
    // package referenced. The analyzer resolves "SIL.Harmony.Changes.IChange" by metadata name,
    // which finds source-declared types just as it would metadata ones.
    private const string HarmonyStub = """
        namespace SIL.Harmony.Changes
        {
            public interface IChange { }
            public abstract class Change<T> : IChange { protected Change(System.Guid entityId) { } }
            public abstract class CreateChange<T> : Change<T> { protected CreateChange(System.Guid entityId) : base(entityId) { } }
            public abstract class EditChange<T> : Change<T> { protected EditChange(System.Guid entityId) : base(entityId) { } }
        }
        """;

    // Test code (with its using directives) comes first; the stub namespace is appended after,
    // since using directives must precede all namespace declarations in a compilation unit.
    private static Task VerifyAsync(string testCode) => Verify.VerifyAnalyzerAsync(testCode + "\n" + HarmonyStub);

    [Fact]
    public Task Passes_WhenChangeDeclaresGuidEntityIdConstructor() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class GoodChange : EditChange<object>
            {
                public GoodChange(Guid entityId) : base(entityId) { }
            }
        }
        """);

    [Fact]
    public Task Passes_WhenEntityIdIsInAPrimaryConstructorAlongsideOtherParameters() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class GoodPrimaryChange(string value, Guid entityId) : EditChange<object>(entityId)
            {
                public string Value { get; } = value;
            }
        }
        """);

    // Only one constructor needs the `Guid entityId` parameter. Additional constructors without it
    // are valid — this mirrors the CreateEntryChange pattern (a public domain-object constructor plus
    // a private [JsonConstructor] taking Guid entityId).
    [Fact]
    public Task Passes_WhenOnlyOneOfMultipleConstructorsTakesGuidEntityId() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class CreateThingChange : CreateChange<object>
            {
                public CreateThingChange(string value) : base(Guid.NewGuid())
                {
                    Value = value;
                }

                private CreateThingChange(Guid entityId) : base(entityId) { }

                public string? Value { get; set; }
            }
        }
        """);

    [Fact]
    public Task Passes_WhenChangeIsCreatedViaCreateChangeBase() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class CreateFooChange(Guid entityId) : CreateChange<object>(entityId) { }
        }
        """);

    [Fact]
    public Task Flags_WhenConstructorParameterIsNotNamedEntityId() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class {|LX0001:WrongNameChange|} : EditChange<object>
            {
                // 'id' does not match the EntityId property, so JSON deserialization would fail.
                public WrongNameChange(Guid id) : base(id) { }
            }
        }
        """);

    [Fact]
    public Task Flags_WhenChangeImplementsIChangeButHasNoEntityIdConstructor() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class {|LX0001:DirectChange|} : IChange
            {
                public DirectChange(string name) { }
            }
        }
        """);

    // Near-miss the analyzer must stay silent on: an unrelated type that happens to have a
    // constructor without an entityId parameter but does not implement IChange.
    [Fact]
    public Task Ignores_TypeThatIsNotAChange() => VerifyAsync("""
        using System;
        namespace TestChanges
        {
            public class NotAChange
            {
                public NotAChange(string name) { }
            }
        }
        """);

    // Abstract change bases can't be deserialized directly, so they're exempt even without the ctor.
    [Fact]
    public Task Ignores_AbstractChangeType() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public abstract class AbstractChange : IChange
            {
                protected AbstractChange(string name) { }
            }
        }
        """);

    // The rule must not fire in generated code (ConfigureGeneratedCodeAnalysis(None)).
    [Fact]
    public Task Ignores_GeneratedCode() => Verify.VerifyAnalyzerAsync("""
        // <auto-generated/>
        namespace SIL.Harmony.Changes
        {
            public interface IChange { }
        }
        namespace TestChanges
        {
            public class GeneratedBadChange : SIL.Harmony.Changes.IChange
            {
                public GeneratedBadChange(string name) { }
            }
        }
        """);
}

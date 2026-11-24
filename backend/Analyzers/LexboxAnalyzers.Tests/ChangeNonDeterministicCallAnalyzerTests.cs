using System.Threading.Tasks;
using Xunit;
using Verify = Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerVerifier<
    LexboxAnalyzers.Rules.ChangeNonDeterministicCallAnalyzer,
    Microsoft.CodeAnalysis.Testing.DefaultVerifier>;

namespace LexboxAnalyzers.Tests;

public class ChangeNonDeterministicCallAnalyzerTests
{
    // Minimal in-source stand-in for the Harmony change hierarchy. The analyzer only keys off
    // "SIL.Harmony.Changes.IChange" (resolved by metadata name, which finds source-declared types
    // too); the method names and return types below are illustrative, not something it matches on.
    private const string HarmonyStub = """
        namespace SIL.Harmony.Changes
        {
            public interface IChangeContext { }
            public interface IChange { }
            public abstract class Change<T> : IChange
            {
                protected Change(System.Guid entityId) { EntityId = entityId; }
                public System.Guid EntityId { get; set; }
                public abstract void ApplyChange(T entity, IChangeContext context);
            }
            public abstract class CreateChange<T> : Change<T>
            {
                protected CreateChange(System.Guid entityId) : base(entityId) { }
                public abstract T NewEntity(IChangeContext context);
                public override void ApplyChange(T entity, IChangeContext context) { }
            }
            public abstract class EditChange<T> : Change<T>
            {
                protected EditChange(System.Guid entityId) : base(entityId) { }
            }
        }
        """;

    // Test code (with its using directives) comes first; the stub namespace is appended after,
    // since using directives must precede all namespace declarations in a compilation unit.
    private static Task VerifyAsync(string testCode) => Verify.VerifyAnalyzerAsync(testCode + "\n" + HarmonyStub);

    [Fact]
    public Task Flags_GuidNewGuidInApplyChange() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Item { public Guid Id { get; set; } }
            public class BadChange(Guid entityId) : EditChange<Item>(entityId)
            {
                public override void ApplyChange(Item entity, IChangeContext context)
                {
                    entity.Id = {|LX0002:Guid.NewGuid()|};
                }
            }
        }
        """);

    // The shape of the original bug: ApplyChange delegates to a private static helper, and the
    // fresh Guid is minted inside an object initializer in that helper. Helpers are checked because
    // the analyzer can't tell which of them end up on the replay path.
    [Fact]
    public Task Flags_NonDeterministicCallInPrivateHelperCalledFromApplyChange() => VerifyAsync("""
        using System;
        using System.Collections.Generic;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Translation { public Guid Id { get; set; } }
            public class Sentence { public List<Translation> Translations { get; } = new(); }
            public class PatchSentenceChange(Guid entityId) : EditChange<Sentence>(entityId)
            {
                public override void ApplyChange(Sentence entity, IChangeContext context)
                {
                    ApplyTranslationOp(entity);
                }

                private static void ApplyTranslationOp(Sentence entity)
                {
                    if (entity.Translations.Count == 0)
                        entity.Translations.Add(new Translation { Id = {|LX0002:Guid.NewGuid()|} });
                }
            }
        }
        """);

    [Fact]
    public Task Flags_ClockReadsInNewEntity() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Thread { public DateTimeOffset CreatedAt { get; set; } public DateTime Seen { get; set; } }
            public class CreateThreadChange(Guid entityId) : CreateChange<Thread>(entityId)
            {
                public override Thread NewEntity(IChangeContext context)
                {
                    return new Thread
                    {
                        CreatedAt = {|LX0002:DateTimeOffset.UtcNow|},
                        Seen = {|LX0002:DateTime.Now|}.AddDays(-1),
                    };
                }
            }
        }
        """);

    // Random is banned wholesale: constructing one and every call on the instance.
    [Fact]
    public Task Flags_RandomConstructionAndUse() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Item { public int Order { get; set; } }
            public class ShuffleChange(Guid entityId) : EditChange<Item>(entityId)
            {
                public override void ApplyChange(Item entity, IChangeContext context)
                {
                    var random = {|LX0002:new Random()|};
                    entity.Order = {|LX0002:random.Next()|};
                }
            }
        }
        """);

    [Fact]
    public Task Flags_EnvironmentAndStopwatch() => VerifyAsync("""
        using System;
        using System.Diagnostics;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Item { public string Note { get; set; } = ""; public long Stamp { get; set; } }
            public class StampChange(Guid entityId) : EditChange<Item>(entityId)
            {
                public override void ApplyChange(Item entity, IChangeContext context)
                {
                    entity.Note = "a" + {|LX0002:Environment.NewLine|} + "b";
                    entity.Stamp = {|LX0002:Stopwatch.GetTimestamp()|} + {|LX0002:Environment.TickCount|};
                }
            }
        }
        """);

    // A property initializer runs on deserialization too, so if the property is absent from the
    // serialized change (e.g. it was added later), every client ends up with its own value.
    [Fact]
    public Task Flags_PropertyAndFieldInitializers() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class InitChange(Guid entityId) : EditChange<object>(entityId)
            {
                public Guid NewId { get; set; } = {|LX0002:Guid.NewGuid()|};
                private readonly DateTime _created = {|LX0002:DateTime.UtcNow|};
                public override void ApplyChange(object entity, IChangeContext context) { }
            }
        }
        """);

    [Fact]
    public Task Flags_InsideLambdaLocalFunctionAndMethodGroup() => VerifyAsync("""
        using System;
        using System.Collections.Generic;
        using System.Linq;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Item { public Guid Id { get; set; } }
            public class Bag { public List<Item> Items { get; } = new(); }
            public class RenumberChange(Guid entityId) : EditChange<Bag>(entityId)
            {
                public override void ApplyChange(Bag entity, IChangeContext context)
                {
                    Func<Guid> viaMethodGroup = {|LX0002:Guid.NewGuid|};
                    entity.Items.ForEach(i => i.Id = {|LX0002:Guid.NewGuid()|});
                    Guid Local() => {|LX0002:Guid.NewGuid()|};
                    entity.Items.First().Id = Local();
                }
            }
        }
        """);

    // Unlike LX0001, abstract change bases are not exempt: their helpers run on replay through
    // every derived change.
    [Fact]
    public Task Flags_HelperInAbstractChangeBase() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public abstract class TimestampedChange(Guid entityId) : EditChange<object>(entityId)
            {
                protected DateTimeOffset Stamp() => {|LX0002:DateTimeOffset.Now|};
            }
        }
        """);

    [Fact]
    public Task Flags_InNestedHelperTypeOfChange() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class OuterChange(Guid entityId) : EditChange<object>(entityId)
            {
                public override void ApplyChange(object entity, IChangeContext context) => Helper.Next();

                private static class Helper
                {
                    public static Guid Next() => {|LX0002:Guid.NewGuid()|};
                }
            }
        }
        """);

    // The constructor runs once, on the originating client; the generated values are serialized
    // and replayed verbatim everywhere else. This mirrors CreateEntryChange / CreateCommentThreadChange.
    [Fact]
    public Task Ignores_NonDeterministicCallsInConstructor() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Thread { public Guid Id { get; set; } public DateTimeOffset CreatedAt { get; set; } }
            public class CreateThreadChange : CreateChange<Thread>
            {
                public CreateThreadChange(Thread thread) : base(thread.Id == Guid.Empty ? Guid.NewGuid() : thread.Id)
                {
                    CreatedAt = thread.CreatedAt == default ? DateTimeOffset.UtcNow : thread.CreatedAt;
                }

                private CreateThreadChange(Guid entityId) : base(entityId) { }

                public DateTimeOffset CreatedAt { get; set; }

                public override Thread NewEntity(IChangeContext context) => new() { Id = EntityId, CreatedAt = CreatedAt };
            }
        }
        """);

    [Fact]
    public Task Ignores_NonDeterministicCallInPrimaryConstructorBaseInitializer() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class CreateThingChange(string value) : CreateChange<object>(Guid.NewGuid())
            {
                public string Value { get; } = value;
                public override object NewEntity(IChangeContext context) => Value;
            }
        }
        """);

    // Near-miss: the same calls in a type that isn't a change are fine.
    [Fact]
    public Task Ignores_TypeThatIsNotAChange() => VerifyAsync("""
        using System;
        namespace TestChanges
        {
            public class ItemService
            {
                public Guid Create() => Guid.NewGuid();
                public DateTime Now() => DateTime.UtcNow;
                public int Roll() => new Random().Next();
            }
        }
        """);

    // Near-miss: deterministic members of the banned types must stay allowed.
    [Fact]
    public Task Ignores_DeterministicMembersOfBannedTypes() => VerifyAsync("""
        using System;
        using SIL.Harmony.Changes;
        namespace TestChanges
        {
            public class Item { public Guid Id { get; set; } public DateTime When { get; set; } public string Path { get; set; } = ""; }
            public class FixedChange(Guid entityId) : EditChange<Item>(entityId)
            {
                public override void ApplyChange(Item entity, IChangeContext context)
                {
                    entity.Id = entity.Id == Guid.Empty ? Guid.Parse("3dce1982-8e93-44f1-b92c-e9c7bdf72801") : new Guid(entity.Id.ToByteArray());
                    entity.When = DateTime.MinValue.AddTicks(DateTimeOffset.FromUnixTimeSeconds(0).Ticks);
                    entity.Path = System.IO.Path.Combine("a", "b");
                    var name = nameof(DateTime.Now) + nameof(Guid.NewGuid);
                }
            }
        }
        """);

    [Fact]
    public Task Ignores_WhenHarmonyIsNotReferenced() => Verify.VerifyAnalyzerAsync("""
        using System;
        namespace TestChanges
        {
            public class LooksLikeAChange
            {
                public Guid Apply() => Guid.NewGuid();
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
            public class GeneratedChange : SIL.Harmony.Changes.IChange
            {
                public System.Guid Apply() => System.Guid.NewGuid();
            }
        }
        """);
}

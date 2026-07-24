using SIL.Harmony.Config;
using FluentAssertions.Execution;
using LcmCrdt.Changes;
using LcmCrdt.Changes.Entries;
using MiniLcm.Media;
using SIL.Harmony.Changes;
using SIL.Harmony.Resource;

namespace LcmCrdt.Tests;

public class ConfigRegistrationTests
{
    private readonly HashSet<Type> ExcludedObjectTypes = []; // e.g. types that CRDT doesn't support yet

    private readonly HashSet<Type> _excludedChangeTypes =
    [
        typeof(JsonPatchChange<ComplexFormComponent>), //not supported
        typeof(JsonPatchChange<RemoteResource<LcmFileMetadata>>), //not supported
        typeof(JsonPatchChange<ExampleSentence>), //replaced by JsonPatchExampleSentenceChange
        typeof(JsonPatchChange<CustomView>), //not supported. Use EditCustomViewChange
        typeof(JsonPatchChange<CommentThread>), //not supported. Use SetCommentThreadStatusChange
        typeof(JsonPatchChange<UserComment>), //not supported. Use EditUserCommentChange
        typeof(DeleteChange<MorphType>), //MorphTypes cannot be deleted
        typeof(DeleteChange<RemoteResource<LcmFileMetadata>>)//Not used, instead DeleteRemoteResourceChange is used
    ];

    private readonly HarmonyConfig _config;

    public ConfigRegistrationTests()
    {
        _config = new HarmonyConfig();
        LcmCrdtKernel.ConfigureCrdt(_config);
    }


    [Fact]
    public void AllObjectsAreRegistered()
    {
        var allObjectTypes = typeof(IObjectWithId).Assembly.GetTypes().Where(t =>
            t is { IsClass: true, IsAbstract: false } && t.GetInterface(nameof(IObjectWithId)) is not null).ToArray();
        allObjectTypes.Should().NotBeEmpty();
        var registeredObjectTypes = _config.ObjectTypes.ToHashSet();
        using var _ = new AssertionScope();
        foreach (var allObjectType in allObjectTypes)
        {
            if (ExcludedObjectTypes.Contains(allObjectType)) continue;
            registeredObjectTypes.Should().Contain(allObjectType,
                $"All IObjectWithId types should be added in {nameof(LcmCrdtKernel)}.{nameof(LcmCrdtKernel.ConfigureCrdt)}, if not exclude it in this test");
        }
    }

    [Fact]
    public void AllChangesAreRegistered()
    {
        var registeredObjectTypes = _config.ObjectTypes.ToHashSet();
        var allChangeTypes = typeof(LcmCrdtConfig).Assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.GetInterface(nameof(IChange)) is not null)
            .Append(typeof(DeleteChange<>))
            .ToLookup(t => t.IsGenericType);
        allChangeTypes.Should().NotBeEmpty();
        // Harmony's ChangeTypes now yields RegisteredChangeType descriptors; project back to the CLR types.
        var registeredChangeTypes = _config.ChangeTypes.Select(t => t.Type).ToHashSet();
        using var _ = new AssertionScope();
        //non generics
        foreach (var changeType in allChangeTypes[false])
        {
            if (_excludedChangeTypes.Contains(changeType)) continue;
            registeredChangeTypes.Should().Contain(changeType,
                $"All IChange types should be added in {nameof(LcmCrdtKernel)}.{nameof(LcmCrdtKernel.ConfigureCrdt)}, if not exclude it in this test");
        }

        //generics
        foreach (var genericChangeType in allChangeTypes[true])
        {
            foreach (var objectType in registeredObjectTypes)
            {
                try
                {
                    var actualChangeType = genericChangeType.MakeGenericType(objectType);
                    if (_excludedChangeTypes.Contains(actualChangeType)) continue;
                    registeredChangeTypes.Should().Contain(actualChangeType,
                        $"All IChange types should be added in {nameof(LcmCrdtKernel)}.{nameof(LcmCrdtKernel.ConfigureCrdt)}, if not exclude it in this test");
                }
                catch (ArgumentException e) when (e.InnerException is TypeLoadException)
                {
                    //should land here when the generic type has constraints preventing this object type from working anyway
                }
            }
        }
    }
}

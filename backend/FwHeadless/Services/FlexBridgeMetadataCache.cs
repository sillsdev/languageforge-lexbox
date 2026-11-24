using System.Reflection;

namespace FwHeadless.Services;

/// <summary>
/// FLEx Bridge caches one project's model version and custom field types in a static it never clears, so in a process
/// that handles many projects it validates one project's custom fields against another project's types, and Chorus
/// rolls back the Send/Receive. Remove once LfMergeBridge resets the cache itself (https://github.com/sillsdev/flexbridge/pull/431).
/// </summary>
public static class FlexBridgeMetadataCache
{
    private static readonly Lazy<FieldInfo> CacheField = new(() =>
        Type.GetType("LibFLExBridgeChorusPlugin.Infrastructure.MetadataCache, LibFLExBridge-ChorusPlugin")
            ?.GetField("_mdc", BindingFlags.Static | BindingFlags.NonPublic)
        ?? throw new MissingFieldException("MetadataCache", "_mdc"));

    // Not thread-safe: FwHeadless runs one Send/Receive at a time
    public static void Reset() => CacheField.Value.SetValue(null, null);
}

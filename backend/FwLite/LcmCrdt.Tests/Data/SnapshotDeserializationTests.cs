using System.Runtime.CompilerServices;
using System.Text.Json;
using SIL.Harmony.Core;

namespace LcmCrdt.Tests.Data;

public class SnapshotDeserializationTests
{

    private static readonly Lazy<JsonSerializerOptions> LazyOptions = new(() =>
    {
        var config = LcmCrdtKernel.MakeConfig();
        config.JsonSerializerOptions.ReadCommentHandling = JsonCommentHandling.Skip;
        return config.JsonSerializerOptions;
    });

    private static readonly JsonSerializerOptions Options = LazyOptions.Value;
    [Fact]
    public void CanDeserializeRegressionData()
    {
        //this file represents projects which already have changes applied, we want to ensure that we don't break anything.
        //nothing should ever be removed from this file
        //if a new property is added then a new json object should be added with that property
        using var jsonFile = File.OpenRead(GetJsonFilePath("RegressionDeserializationData.json"));
        var changes = JsonSerializer.Deserialize<List<IObjectBase>>(jsonFile, Options);
        changes.Should().NotBeNullOrEmpty().And.NotContainNulls();
    }

    private static string GetJsonFilePath(string name, [CallerFilePath] string sourceFile = "")
    {
        return Path.Combine(
            Path.GetDirectoryName(sourceFile) ??
            throw new InvalidOperationException("Could not get directory of source file"),
            name);
    }
}

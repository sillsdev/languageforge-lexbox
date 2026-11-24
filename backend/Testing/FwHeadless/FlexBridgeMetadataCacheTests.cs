using Chorus.FileTypeHandlers;
using FluentAssertions;
using FwDataMiniLcmBridge;
using FwDataMiniLcmBridge.LcmUtils;
using FwHeadless.Services;
using Microsoft.Extensions.Options;
using SIL.Progress;

namespace Testing.FwHeadless;

public class FlexBridgeMetadataCacheTests : IDisposable
{
    private readonly DirectoryInfo _workDir = Directory.CreateTempSubdirectory("flexbridge-metadata-cache-test-");
    private readonly string _blankFwdata;

    public FlexBridgeMetadataCacheTests()
    {
        var project = new FwDataProject("blank", _workDir.FullName);
        var loader = new ProjectLoader(Options.Create(new FwDataBridgeConfig
        {
            ProjectsFolder = _workDir.FullName,
            TemplatesFolder = Path.GetFullPath("Templates"),
        }));
        loader.NewProject(project, "en", "en").Dispose();
        _blankFwdata = File.ReadAllText(project.FilePath);
    }

    public void Dispose() => _workDir.Delete(recursive: true);

    [Fact]
    public void ACustomFieldIsValidatedAgainstItsOwnProjectsType()
    {
        Split("first", customFieldType: "MultiUnicode", customFieldValue: """<AUni ws="en">gloss</AUni>""");

        FlexBridgeMetadataCache.Reset();
        var lexiconFiles = Split("second", customFieldType: "String", customFieldValue: """<Str><Run ws="en">plural</Run></Str>""");

        var handlers = ChorusFileTypeHandlerCollection.CreateWithInstalledHandlers().Handlers;
        lexiconFiles.Select(file => handlers.First(h => h.CanValidateFile(file)).ValidateFile(file, new NullProgress()))
            .Should().AllSatisfy(error => error.Should().BeNull());
    }

    private string[] Split(string projectName, string customFieldType, string customFieldValue)
    {
        var projectDir = Directory.CreateDirectory(Path.Combine(_workDir.FullName, projectName));
        var fwdataPath = Path.Combine(projectDir.FullName, $"{projectName}.fwdata");
        var fwdata = _blankFwdata.Replace("</languageproject>", $"""
            <rt class="LexEntry" guid="5c9d2e3f-1a2b-4c3d-8e4f-5a6b7c8d9e01">
            <Custom name="Plural">{customFieldValue}</Custom>
            <DateCreated val="2026-1-1 00:00:00.000" />
            <DateModified val="2026-1-1 00:00:00.000" />
            <DoNotUseForParsing val="False" />
            <HomographNumber val="0" />
            </rt>
            </languageproject>
            """);
        var customField = $"""<CustomField class="LexEntry" name="Plural" type="{customFieldType}" wsSelector="-2" />""";
        fwdata = fwdata.Insert(fwdata.IndexOf("<rt ", StringComparison.Ordinal), $"<AdditionalFields>{customField}</AdditionalFields>\n");
        File.WriteAllText(fwdataPath, fwdata);

        LfMergeBridge.LfMergeBridge.DisassembleFwdataFile(new NullProgress(), false, fwdataPath);
        return Directory.GetFiles(Path.Combine(projectDir.FullName, "Linguistics", "Lexicon"), "*.lexdb");
    }
}

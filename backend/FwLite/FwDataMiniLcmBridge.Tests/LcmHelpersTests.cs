using FwDataMiniLcmBridge.Api;
using FwDataMiniLcmBridge.Tests.Fixtures;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Tests;

[Collection(ProjectLoaderFixture.Name)]
public class LcmHelpersTests
{
    private FwDataMiniLcmApi _fwDataMiniLcmApi;

    public LcmHelpersTests(ProjectLoaderFixture fixture)
    {
        _fwDataMiniLcmApi = fixture.NewProjectApi("lcm-helpers-test", "en", "en");
    }

    [Fact]
    public async Task PickTextMustNotReturnNull()
    {
        var id = Guid.NewGuid();
        await _fwDataMiniLcmApi.CreateEntry(new Entry(){Id = id});
        var entry = (ILexEntry) _fwDataMiniLcmApi.Cache.ServiceLocator.ObjectRepository.GetObject(id);
        //must not return null, because of https://github.com/sillsdev/languageforge-lexbox/issues/1874
        entry.PickText(entry.LexemeFormOA.Form, "en").Should().NotBeNull().And.BeEmpty();
    }
}

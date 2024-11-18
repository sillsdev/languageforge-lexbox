using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.LCModel.DomainServices;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateWritingSystemProxy : WritingSystem
{
    private readonly CoreWritingSystemDefinition _origLcmWritingSystem;
    private readonly CoreWritingSystemDefinition _workingLcmWritingSystem;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    public UpdateWritingSystemProxy(CoreWritingSystemDefinition lcmWritingSystem, FwDataMiniLcmApi lexboxLcmApi)
    {
        _origLcmWritingSystem = lcmWritingSystem;
        _workingLcmWritingSystem = new CoreWritingSystemDefinition(lcmWritingSystem, cloneId: true);
        _lexboxLcmApi = lexboxLcmApi;
    }

    public void CommitUpdate(LcmCache cache)
    {
        if (_workingLcmWritingSystem.Id == _origLcmWritingSystem.Id)
        {
            cache.ServiceLocator.WritingSystemManager.Set(_workingLcmWritingSystem);
        }
        else
        {
            // Changing the ID of a writing system requires LCM to do a lot of work, so only go through that process if absolutely required
            WritingSystemServices.MergeWritingSystems(cache, _workingLcmWritingSystem, _origLcmWritingSystem);
        }
    }

    public override required WritingSystemId WsId
    {
        get => _workingLcmWritingSystem.Id;
        set => _workingLcmWritingSystem.Id = value;
    }

    public override required string Name
    {
        get => _workingLcmWritingSystem.LanguageName;
        set { } // Silently do nothing; name should be derived from WsId at all times, so if the name should change then so should the WsId
    }

    public override required string Abbreviation
    {
        get => _workingLcmWritingSystem.Abbreviation;
        set => _workingLcmWritingSystem.Abbreviation = value;
    }

    // TODO: Change WritingSystem Font property to be a list of strings instead of a single string, then do something like this
    // public override required List<string> Fonts
    // {
    //     get => _workingLcmWritingSystem.Fonts.Select(f => f.Name).ToList();
    //     set => throw new NotImplementedException();
    // }
}

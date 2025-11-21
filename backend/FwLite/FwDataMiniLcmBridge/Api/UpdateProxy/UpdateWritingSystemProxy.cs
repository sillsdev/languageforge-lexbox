using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateWritingSystemProxy : WritingSystem
{
    private readonly CoreWritingSystemDefinition _lcmWritingSystem;

    [SetsRequiredMembers]
    public UpdateWritingSystemProxy(CoreWritingSystemDefinition lcmWritingSystem)
    {
        _lcmWritingSystem = lcmWritingSystem;
        base.Abbreviation = Abbreviation = _lcmWritingSystem.Abbreviation ?? "";
        base.Name = Name = _lcmWritingSystem.LanguageName ?? "";
        base.Font = Font = _lcmWritingSystem.DefaultFontName ?? "";
    }

    public override required WritingSystemId WsId
    {
        get => _lcmWritingSystem.Id;
        set => throw new NotSupportedException("Changing the ID of a writing system is not supported");
    }

    public override required string Name
    {
        get => _lcmWritingSystem.LanguageName;
        set { } // Silently do nothing; name should be derived from WsId at all times, so if the name should change then so should the WsId
    }

    public override required string Abbreviation
    {
        get => _lcmWritingSystem.Abbreviation;
        set => _lcmWritingSystem.Abbreviation = value;
    }

    public override required string Font
    {
        get => _lcmWritingSystem.DefaultFontName;
        set
        {
            if (value != _lcmWritingSystem.DefaultFontName)
            {
                _lcmWritingSystem.DefaultFont = new FontDefinition(value);
            }
        }
    }
}

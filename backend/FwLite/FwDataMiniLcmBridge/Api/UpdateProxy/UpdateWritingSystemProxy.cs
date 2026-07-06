using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.WritingSystems;
using SIL.WritingSystems;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateWritingSystemProxy : WritingSystem
{
    private readonly CoreWritingSystemDefinition _lcmWritingSystem;
    private readonly IWritingSystemContainer _writingSystemContainer;

    [SetsRequiredMembers]
    public UpdateWritingSystemProxy(CoreWritingSystemDefinition lcmWritingSystem, IWritingSystemContainer writingSystemContainer)
    {
        _lcmWritingSystem = lcmWritingSystem;
        _writingSystemContainer = writingSystemContainer;
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

    public override bool IsDisabled
    {
        get => !CurrentList.Contains(_lcmWritingSystem);
        set
        {
            if (value == IsDisabled) return;
            if (value)
            {
                if (CurrentList.Count == 1)
                    throw new InvalidOperationException(
                        $"Cannot disable the last enabled {Type} writing system ({WsId})");
                CurrentList.Remove(_lcmWritingSystem);
            }
            else
            {
                switch (Type)
                {
                    case WritingSystemType.Analysis:
                        _writingSystemContainer.AddToCurrentAnalysisWritingSystems(_lcmWritingSystem);
                        break;
                    case WritingSystemType.Vernacular:
                        _writingSystemContainer.AddToCurrentVernacularWritingSystems(_lcmWritingSystem);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(Type), Type, null);
                }
            }
        }
    }

    private IList<CoreWritingSystemDefinition> CurrentList => Type switch
    {
        WritingSystemType.Analysis => _writingSystemContainer.CurrentAnalysisWritingSystems,
        WritingSystemType.Vernacular => _writingSystemContainer.CurrentVernacularWritingSystems,
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, null)
    };
}

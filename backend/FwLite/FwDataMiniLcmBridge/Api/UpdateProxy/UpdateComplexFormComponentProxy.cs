using System.Diagnostics.CodeAnalysis;
using MiniLcm.Models;
using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateComplexFormComponentProxy : ComplexFormComponent
{
    private readonly ILexEntry _lexComplexForm;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;
    private readonly ILexEntry _lexComponentEntry;
    private readonly ILexSense? _lexSense;

    [SetsRequiredMembers]
    public UpdateComplexFormComponentProxy(ILexEntry lexComplexForm, ICmObject o, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lexComplexForm = lexComplexForm;
        _lexboxLcmApi = lexboxLcmApi;
        _lexSense = o as ILexSense;
        _lexComponentEntry = _lexSense?.Entry ?? (ILexEntry)o;
    }

    public override required Guid ComplexFormEntryId
    {
        get => _lexComplexForm.Guid;
        set
        {
            _lexboxLcmApi.RemoveComplexFormComponent(_lexComplexForm, this);
            _lexboxLcmApi.EntriesRepository.GetObject(value).AddComponent((_lexSense as ICmObject ?? _lexComponentEntry));
        }
    }

    public override required Guid ComponentEntryId
    {
        get => _lexComponentEntry.Guid;
        set
        {
            _lexboxLcmApi.RemoveComplexFormComponent(_lexComplexForm, this);
            _lexboxLcmApi.AddComplexFormComponent(_lexComplexForm,
                new ComplexFormComponent { ComplexFormEntryId = _lexComplexForm.Guid, ComponentEntryId = value, });
        }
    }

    public override Guid? ComponentSenseId
    {
        get => _lexSense?.Guid;
        set
        {
            _lexboxLcmApi.RemoveComplexFormComponent(_lexComplexForm, this);
            if (value is null)
            {
                _lexboxLcmApi.AddComplexFormComponent(_lexComplexForm,
                    new ComplexFormComponent
                    {
                        ComplexFormEntryId = _lexComplexForm.Guid,
                        ComponentEntryId = _lexComponentEntry.Guid
                    });
                return;
            }
            _lexboxLcmApi.AddComplexFormComponent(_lexComplexForm,
                new ComplexFormComponent
                {
                    ComplexFormEntryId = _lexComplexForm.Guid,
                    ComponentSenseId = value,
                    ComponentEntryId = _lexboxLcmApi.SenseRepository.GetObject(value.Value).Entry.Guid
                });
        }
    }
}

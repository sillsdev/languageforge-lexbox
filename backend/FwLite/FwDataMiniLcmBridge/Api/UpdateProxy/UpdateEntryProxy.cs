using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateEntryProxy : Entry
{
    private readonly ILexEntry _lcmEntry;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    private readonly Lazy<UpdateComplexFormComponentProxy[]> _lazyComplexForms;

    public UpdateEntryProxy(ILexEntry lcmEntry, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmEntry = lcmEntry;
        Id = lcmEntry.Guid;
        _lexboxLcmApi = lexboxLcmApi;
        _lazyComplexForms = new(() =>
        {
            return _lcmEntry.ComplexFormEntries.Select(complexEntry =>
                    new UpdateComplexFormComponentProxy(complexEntry, _lcmEntry, _lexboxLcmApi))
                .Concat(
                    _lcmEntry.AllSenses.SelectMany(sense => sense.ComplexFormEntries.Select(complexEntry =>
                        new UpdateComplexFormComponentProxy(complexEntry, sense, _lexboxLcmApi)))
                ).ToArray();
        });
    }

    public override MultiString LexemeForm
    {
        get => new UpdateMultiStringProxy(_lcmEntry.LexemeFormOA.Form, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString CitationForm
    {
        get => new UpdateMultiStringProxy(_lcmEntry.CitationForm, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString LiteralMeaning
    {
        get => new UpdateMultiStringProxy(_lcmEntry.LiteralMeaning, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override IList<Sense> Senses
    {
        get =>
            new UpdateListProxy<Sense>(
                sense => _lexboxLcmApi.CreateSense(_lcmEntry, sense),
                sense => _lexboxLcmApi.DeleteSense(Id, sense.Id),
                i => new UpdateSenseProxy(_lcmEntry.SensesOS[i], _lexboxLcmApi),
                _lcmEntry.SensesOS.Count
            );
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormComponent> Components
    {
        get =>
            new UpdateListProxy<ComplexFormComponent>(
                component => _lexboxLcmApi.AddComplexFormComponent(_lcmEntry, component),
                component => _lexboxLcmApi.RemoveComplexFormComponent(_lcmEntry, component),
                i => new UpdateComplexFormComponentProxy(_lcmEntry,
                    _lcmEntry.ComplexFormEntryRefs.Single().ComponentLexemesRS[i],
                    _lexboxLcmApi),
                _lcmEntry.ComplexFormEntryRefs.SingleOrDefault()?.ComponentLexemesRS.Count ?? 0
            );
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormComponent> ComplexForms
    {
        get =>
            new UpdateListProxy<ComplexFormComponent>(
                component => _lexboxLcmApi.AddComplexFormComponent(_lexboxLcmApi.EntriesRepository.GetObject(component.ComplexFormEntryId), component),
                component => _lexboxLcmApi.RemoveComplexFormComponent(_lexboxLcmApi.EntriesRepository.GetObject(component.ComplexFormEntryId), component),
                //todo this does not handle complex forms which reference a sense
                i => _lazyComplexForms.Value[i],
                _lazyComplexForms.Value.Length
            );
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormType> ComplexFormTypes
    {
        get =>
            new UpdateListProxy<ComplexFormType>(
                complexFormType => _lexboxLcmApi.AddComplexFormType(_lcmEntry, complexFormType.Id),
                complexFormType => _lexboxLcmApi.RemoveComplexFormType(_lcmEntry, complexFormType.Id),
                i => new UpdateComplexFormTypeProxy(_lcmEntry.ComplexFormEntryRefs.Single().ComplexEntryTypesRS[i], _lcmEntry, _lexboxLcmApi),
                _lcmEntry.ComplexFormEntryRefs.SingleOrDefault()
                    ?.ComplexEntryTypesRS.Count ?? 0
            );
        set => throw new NotImplementedException();
    }

    public override MultiString Note
    {
        get => new UpdateMultiStringProxy(_lcmEntry.Comment, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }
}

public class UpdateMultiStringProxy(ITsMultiString multiString, FwDataMiniLcmApi lexboxLcmApi) : MultiString
{
    public override IDictionary<WritingSystemId, string> Values { get; } =
        new UpdateDictionaryProxy(multiString, lexboxLcmApi);

    public override MultiString Copy()
    {
        return new UpdateMultiStringProxy(multiString, lexboxLcmApi);
    }
}

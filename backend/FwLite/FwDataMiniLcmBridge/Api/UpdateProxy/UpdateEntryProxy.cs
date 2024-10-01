using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public class UpdateEntryProxy(ILexEntry lcmEntry, FwDataMiniLcmApi lexboxLcmApi) : Entry
{
    public override Guid Id
    {
        get => lcmEntry.Guid;
        set => throw new NotImplementedException();
    }

    public override MultiString LexemeForm
    {
        get => new UpdateMultiStringProxy(lcmEntry.LexemeFormOA.Form, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString CitationForm
    {
        get => new UpdateMultiStringProxy(lcmEntry.CitationForm, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MultiString LiteralMeaning
    {
        get => new UpdateMultiStringProxy(lcmEntry.LiteralMeaning, lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override IList<Sense> Senses
    {
        get =>
            new UpdateListProxy<Sense>(
                sense => lexboxLcmApi.CreateSense(lcmEntry, sense),
                sense => lexboxLcmApi.DeleteSense(Id, sense.Id),
                i => new UpdateSenseProxy(lcmEntry.SensesOS[i], lexboxLcmApi),
                lcmEntry.SensesOS.Count
            );
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormComponent> Components
    {
        get =>
            new UpdateListProxy<ComplexFormComponent>(
                component => lexboxLcmApi.AddComplexFormComponent(lcmEntry, component),
                component => lexboxLcmApi.RemoveComplexFormComponent(lcmEntry, component),
                i => new UpdateComplexFormComponentProxy(lcmEntry,
                    lcmEntry.ComplexFormEntryRefs.Single().ComponentLexemesRS[i],
                    lexboxLcmApi),
                lcmEntry.ComplexFormEntryRefs.SingleOrDefault()?.ComponentLexemesRS.Count ?? 0
            );
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormComponent> ComplexForms
    {
        get =>
            new UpdateListProxy<ComplexFormComponent>(
                component => lexboxLcmApi.AddComplexFormComponent(lexboxLcmApi.EntriesRepository.GetObject(component.ComplexFormEntryId), component),
                component => lexboxLcmApi.RemoveComplexFormComponent(lexboxLcmApi.EntriesRepository.GetObject(component.ComplexFormEntryId), component),
                //todo this does not handle complex forms which reference a sense
                i => new UpdateComplexFormComponentProxy(lcmEntry.ComplexFormEntries.ElementAt(i),
                    lcmEntry,
                    lexboxLcmApi),
                lcmEntry.ComplexFormEntries.Count()
            );
        set => throw new NotImplementedException();
    }

    public override MultiString Note
    {
        get => new UpdateMultiStringProxy(lcmEntry.Comment, lexboxLcmApi);
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

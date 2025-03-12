using MiniLcm.Models;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace FwDataMiniLcmBridge.Api.UpdateProxy;

public record UpdateEntryProxy : Entry
{
    private readonly ILexEntry _lcmEntry;
    private readonly FwDataMiniLcmApi _lexboxLcmApi;

    public UpdateEntryProxy(ILexEntry lcmEntry, FwDataMiniLcmApi lexboxLcmApi)
    {
        _lcmEntry = lcmEntry;
        Id = lcmEntry.Guid;
        _lexboxLcmApi = lexboxLcmApi;
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

    public override List<Sense> Senses
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override List<ComplexFormComponent> Components
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override List<ComplexFormComponent> ComplexForms
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override IList<ComplexFormType> ComplexFormTypes
    {
        get => throw new NotImplementedException();
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

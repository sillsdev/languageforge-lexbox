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
        get
        {
            var form = _lcmEntry.LexemeFormOA ?? LcmHelpers.SetLexemeForm(
                _lcmEntry,
                LcmHelpers.FromLcmMorphType(_lcmEntry.PrimaryMorphType),
                _lexboxLcmApi.Cache);
            return new UpdateMultiStringProxy(form.Form, _lexboxLcmApi);
        }
        set => throw new NotImplementedException();
    }

    public override MultiString CitationForm
    {
        get => new UpdateMultiStringProxy(_lcmEntry.CitationForm, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override RichMultiString LiteralMeaning
    {
        get => new UpdateRichMultiStringProxy(_lcmEntry.LiteralMeaning, _lexboxLcmApi);
        set => throw new NotImplementedException();
    }

    public override MorphType MorphType
    {
        get => throw new NotImplementedException();
        set => Console.WriteLine("setting MorphType not implemented"); // Not throwing, for now
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

    public override List<ComplexFormType> ComplexFormTypes
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override List<Publication> PublishIn
    {
        get => throw new NotImplementedException();
        set => throw new NotImplementedException();
    }

    public override RichMultiString Note
    {
        get => new UpdateRichMultiStringProxy(_lcmEntry.Comment, _lexboxLcmApi);
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


public class UpdateRichMultiStringProxy(ITsMultiString multiString, FwDataMiniLcmApi lexboxLcmApi) : RichMultiString(new
    UpdateRichMultiStringDictionaryProxy(multiString, lexboxLcmApi))
{
}

using System.Collections;
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
            _lcmEntry.LexemeFormOA ??= _lexboxLcmApi.Cache.CreateLexemeForm();
            return new UpdateMultiStringProxy(_lcmEntry.LexemeFormOA.Form, _lexboxLcmApi);
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


public class UpdateRichMultiStringProxy(ITsMultiString multiString, FwDataMiniLcmApi lexboxLcmApi) : RichMultiString, IDictionary
{
    private IDictionary<WritingSystemId, string> proxy => new UpdateDictionaryProxy(multiString, lexboxLcmApi);

    void IDictionary.Add(object key, object? value)
    {
        ((IDictionary)proxy).Add(key, value);
    }

    void IDictionary.Remove(object key)
    {
        ((IDictionary)proxy).Remove(key);
    }

    object? IDictionary.this[object key]
    {
        get => ((IDictionary)proxy)[key];
        set => ((IDictionary)proxy)[key] = value;
    }
}

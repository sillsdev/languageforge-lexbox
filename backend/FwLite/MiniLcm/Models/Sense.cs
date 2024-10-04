namespace MiniLcm.Models;

/// <summary>
/// Contains a definition for an entry
/// </summary>
public class Sense : IObjectWithId
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Definition { get; set; } = new();
    public virtual MultiString Gloss { get; set; } = new();
    public virtual string PartOfSpeech { get; set; } = string.Empty;
    public virtual Guid? PartOfSpeechId { get; set; }
    public virtual IList<SemanticDomain> SemanticDomains { get; set; } = [];
    public virtual IList<ExampleSentence> ExampleSentences { get; set; } = [];
}

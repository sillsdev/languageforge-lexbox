namespace MiniLcm;

public class Sense
{
    public virtual Guid Id { get; set; }
    public virtual MultiString Definition { get; set; } = new();
    public virtual MultiString Gloss { get; set; } = new();
    public virtual string PartOfSpeech { get; set; } = string.Empty;
    public virtual IList<string> SemanticDomain { get; set; } = [];
    public virtual IList<ExampleSentence> ExampleSentences { get; set; } = [];
}

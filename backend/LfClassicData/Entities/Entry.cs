namespace LfClassicData.Entities;

public class Entry: EntityDocument<Entry>
{
    public Guid Guid { get; set; }
    public required Dictionary<string, MultiTextValue> Lexeme { get; set; }
    public string? MorphologyType { get; set; }
}

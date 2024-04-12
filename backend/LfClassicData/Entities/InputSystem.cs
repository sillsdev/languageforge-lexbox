namespace LfClassicData.Entities;

public class InputSystem : EntityBase
{
    public required string Abbreviation { get; set; }
    public required string Tag { get; set; }
    public required string LanguageName { get; set; }
    public required bool IsRightToLeft { get; set; }
    public bool VernacularWS { get; set; }
    public bool AnalysisWS { get; set; }
}

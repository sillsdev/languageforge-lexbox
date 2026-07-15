namespace LexCore.Entities;

/// <summary>
/// Body of the internal FwHeadless "create project from template" call. The vernacular/analysis
/// lists and the UI writing system are already validated and defaulted by LexBoxApi (at least one
/// vernacular; analysis and UI default to "en").
/// </summary>
public record CreateProjectFromTemplateInput(
    IReadOnlyList<string> WsVernacular,
    IReadOnlyList<string> WsAnalysis,
    string WsUi,
    AnthropologyCategories AnthropologyCategories);

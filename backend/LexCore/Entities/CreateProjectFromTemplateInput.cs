namespace LexCore.Entities;

/// <summary>
/// Body of the internal FwHeadless "create project from template" call. The vernacular/analysis
/// lists are already validated and defaulted by LexBoxApi (at least one vernacular; analysis
/// defaults to "en").
/// </summary>
public record CreateProjectFromTemplateInput(
    IReadOnlyList<string> WsVernacular,
    IReadOnlyList<string> WsAnalysis,
    AnthropologyCategories AnthropologyCategories);

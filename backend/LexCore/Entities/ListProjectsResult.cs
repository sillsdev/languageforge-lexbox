namespace LexCore.Entities;

// TODO: During code review, bikeshed where this record belongs. Needs to be referenceable from FwLiteShared

public record ListProjectsResult(
    FieldWorksLiteProject[] Projects,
    bool CanDownloadByCode);

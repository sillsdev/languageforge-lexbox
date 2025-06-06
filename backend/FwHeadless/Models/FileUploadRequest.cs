namespace FwHeadless.Models;

public record FileUploadRequest(
    string Filename,
    Guid ProjectId
);

public record PostFileResult(Guid guid);

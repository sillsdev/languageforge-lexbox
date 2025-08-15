namespace FwLiteShared.Services;

public interface IFwLinker
{
    string? GetLinkToEntry(Guid entryId, string projectName);
}

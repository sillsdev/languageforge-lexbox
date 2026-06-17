namespace FwLiteShared.Services;

public interface IFwLinker
{
    Task<string?> GetLinkToEntryAsync(Guid entryId, string projectName);
}

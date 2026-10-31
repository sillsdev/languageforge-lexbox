namespace LcmCrdt;

/// <summary>
/// The identity this device uses to author commits and user comments when no Lexbox user is known,
/// e.g. on a device that has only ever synced offline.
/// </summary>
/// <param name="Id">Stable for the device and prefixed with <see cref="AnonymousIdPrefix"/>, so it can't be mistaken for a Lexbox user id.</param>
/// <param name="Name">The display name the user chose, if any.</param>
public record LocalAuthor(string Id, string? Name)
{
    public const string AnonymousIdPrefix = "anonymous:";
}

public interface ILocalAuthorProvider
{
    LocalAuthor? GetLocalAuthor();
}

/// <summary>
/// Default for hosts with no device identity (e.g. FwHeadless): commits keep today's attribution.
/// </summary>
public class NoLocalAuthorProvider : ILocalAuthorProvider
{
    public LocalAuthor? GetLocalAuthor() => null;
}

public record CurrentAuthor(string? Id, string? Name);

public class CurrentAuthorService(CurrentProjectService currentProjectService, ILocalAuthorProvider localAuthorProvider)
{
    /// <summary>
    /// The Lexbox user last seen for this project wins, even offline; otherwise the device's anonymous local author.
    /// </summary>
    public CurrentAuthor GetCurrentAuthor()
    {
        var projectData = currentProjectService.ProjectData;
        if (!string.IsNullOrEmpty(projectData.LastUserId))
            return new CurrentAuthor(projectData.LastUserId, projectData.LastUserName);
        var localAuthor = localAuthorProvider.GetLocalAuthor();
        if (localAuthor is not null)
            return new CurrentAuthor(localAuthor.Id, localAuthor.Name);
        return new CurrentAuthor(null, projectData.LastUserName);
    }

    /// <summary>
    /// True for user comments written by the current author, including ones this device wrote anonymously
    /// before signing into Lexbox: the anonymous id belongs to this device, so they stay editable after sign-in.
    /// </summary>
    public bool IsCurrentAuthor(string? authorId)
    {
        if (string.IsNullOrEmpty(authorId)) return false;
        return authorId == GetCurrentAuthor().Id || authorId == localAuthorProvider.GetLocalAuthor()?.Id;
    }
}

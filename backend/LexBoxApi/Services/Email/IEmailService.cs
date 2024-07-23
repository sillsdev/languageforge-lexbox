using LexBoxApi.Models.Project;
using LexCore.Auth;
using LexCore.Entities;
using MimeKit;

namespace LexBoxApi.Services.Email;

public interface IEmailService
{
    public Task SendForgotPasswordEmail(string emailAddress);

    public Task SendNewAdminEmail(IAsyncEnumerable<User> admins, string newAdminName, string newAdminEmail);

    /// <summary>
    /// Sends a verification email to the user for their email address.
    /// </summary>
    /// <param name="user">The user to verify the email address for.</param>
    /// <param name="newEmail">
    /// If the user is trying to change their address, this is the new email address.
    /// If null, the verification email will be sent to the current email address of the user.
    /// </param>
    public Task SendVerifyAddressEmail(User user, string? newEmail = null);

    /// <summary>
    /// Sends a organization invitation email to a new user, whose account will be created when they accept.
    /// </summary>
    /// <param name="name">The name (real name, NOT username) of user to invite.</param>
    /// <param name="emailAddress">The email address to send the invitation to</param>
    /// <param name="orgId">The GUID of the organization the user is being invited to</param>
    /// <param name="language">The language in which the invitation email should be sent (default English)</param>
    public Task SendCreateAccountWithOrgEmail(
        string emailAddress,
        string managerName,
        Guid orgId,
        OrgRole orgRole,
        string orgName,
        string? language = null);

    /// <summary>
    /// Sends a project invitation email to a new user, whose account will be created when they accept.
    /// </summary>
    /// <param name="name">The name (real name, NOT username) of user to invite.</param>
    /// <param name="emailAddress">The email address to send the invitation to</param>
    /// <param name="projectId">The GUID of the project the user is being invited to</param>
    /// <param name="language">The language in which the invitation email should be sent (default English)</param>
    public Task SendCreateAccountWithProjectEmail(
        string emailAddress,
        string managerName,
        Guid projectId,
        ProjectRole role,
        string projectName,
        string? language = null);

    public Task SendPasswordChangedEmail(User user);

    public Task SendCreateProjectRequestEmail(LexAuthUser user, CreateProjectInput projectInput);
    public Task SendApproveProjectRequestEmail(User user, CreateProjectInput projectInput);
    public Task SendUserAddedEmail(User user, string projectName, string projectCode);
    public Task SendEmailAsync(MimeMessage message);
}

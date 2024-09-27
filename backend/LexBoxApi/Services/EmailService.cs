using System.Diagnostics;
using System.Net.Http.Headers;
using System.Text.Json;
using LexBoxApi.Auth;
using LexBoxApi.Config;
using LexBoxApi.Jobs;
using LexBoxApi.Models.Project;
using LexBoxApi.Otel;
using LexBoxApi.Services.Email;
using LexCore.Auth;
using LexCore.Entities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using OpenTelemetry.Trace;

namespace LexBoxApi.Services;

public class EmailService(
    IOptions<EmailConfig> emailConfig,
    JsonSerializerOptions jsonSerializerOptions,
    IHttpClientFactory clientFactory,
    LexboxLinkGenerator linkGenerator,
    IHttpContextAccessor httpContextAccessor,
    Quartz.ISchedulerFactory schedulerFactory,
    LexAuthService lexAuthService) : IEmailService
{
    private readonly EmailConfig _emailConfig = emailConfig.Value;
    private readonly LinkGenerator _linkGenerator = linkGenerator;

    public async Task SendForgotPasswordEmail(string emailAddress)
    {
        var (lexAuthUser, user) = await lexAuthService.GetUser(emailAddress);
        // we want to silently return if the user doesn't exist, so we don't leak information.
        if (lexAuthUser is null || user?.CanLogin() is not true) return;
        var (jwt, _, lifetime) = lexAuthService.GenerateJwt(lexAuthUser with { Audience = LexboxAudience.ForgotPassword }, true);

        var email = StartUserEmail(user);
        if (email is null) return;
        var httpContext = httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(httpContext);
        // returnTo is a svelte app url
        var forgotLink = _linkGenerator.GetUriByAction(httpContext,
            "LoginRedirect",
            "Login",
            new { jwt, returnTo = "/resetPassword" });
        ArgumentException.ThrowIfNullOrEmpty(forgotLink);
        await RenderEmail(email, new ForgotPasswordEmail(user.Name, forgotLink, lifetime), user.LocalizationCode);
        await SendEmailWithRetriesAsync(email, retryCount: 5, retryWaitSeconds: 30);
    }

    public async Task SendNewAdminEmail(IAsyncEnumerable<User> admins, string newAdminName, string newAdminEmail)
    {
        var email = new MimeMessage();
        await foreach (var admin in admins)
        {
            email.Bcc.Add(new MailboxAddress(admin.Name, admin.Email));
        }
        await RenderEmail(email, new NewAdminEmail("Admin", newAdminName, newAdminEmail), User.DefaultLocalizationCode);
        await SendEmailAsync(email);
    }

    /// <summary>
    /// Sends a verification email to the user for their email address.
    /// </summary>
    /// <param name="user">The user to verify the email address for.</param>
    /// <param name="newEmail">
    /// If the user is trying to change their address, this is the new email address.
    /// If null, the verification email will be sent to the current email address of the user.
    /// </param>
    public async Task SendVerifyAddressEmail(User user, string? newEmail = null)
    {
        var (jwt, _, lifetime) = lexAuthService.GenerateJwt(new LexAuthUser(user)
        {
            EmailVerificationRequired = null,
            Email = newEmail ?? user.Email,
        },
            useEmailLifetime: true
        );
        var email = StartUserEmail(user, newEmail) ?? throw new ArgumentNullException("emailAddress");
        var httpContext = httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(httpContext);
        var queryParam = string.IsNullOrEmpty(newEmail) ? "verifiedEmail" : "changedEmail";
        var verifyLink = _linkGenerator.GetUriByAction(httpContext,
            "VerifyEmail",
            "Login",
            new { jwt, returnTo = $"/user?emailResult={queryParam}", email = newEmail ?? user.Email, });
        ArgumentException.ThrowIfNullOrEmpty(verifyLink);
        await RenderEmail(email, new VerifyAddressEmail(user.Name, verifyLink, !string.IsNullOrEmpty(newEmail), lifetime), user.LocalizationCode);
        await SendEmailWithRetriesAsync(email);
    }

    /// <summary>
    /// Sends a organization invitation email to a new user, whose account will be created when they accept.
    /// </summary>
    /// <param name="name">The name (real name, NOT username) of user to invite.</param>
    /// <param name="emailAddress">The email address to send the invitation to</param>
    /// <param name="orgId">The GUID of the organization the user is being invited to</param>
    /// <param name="language">The language in which the invitation email should be sent (default English)</param>
    public async Task SendCreateAccountWithOrgEmail(
        string emailAddress,
        string managerName,
        Guid orgId,
        OrgRole orgRole,
        string orgName,
        string? language = null)
    {
        language ??= User.DefaultLocalizationCode;
        var authUser = CreateUserForInvite(emailAddress, language);
        authUser.Orgs = [new AuthUserOrg(orgRole, orgId)];
        await SendInvitationEmail(authUser, emailAddress, managerName, orgName, language, isProjectInvitation: false);

    }
    /// <summary>
    /// Sends a project invitation email to a new user, whose account will be created when they accept.
    /// </summary>
    /// <param name="name">The name (real name, NOT username) of user to invite.</param>
    /// <param name="emailAddress">The email address to send the invitation to</param>
    /// <param name="projectId">The GUID of the project the user is being invited to</param>
    /// <param name="language">The language in which the invitation email should be sent (default English)</param>
    public async Task SendCreateAccountWithProjectEmail(
        string emailAddress,
        string managerName,
        Guid projectId,
        ProjectRole role,
        string projectName,
        string? language = null)
    {
        language ??= User.DefaultLocalizationCode;
        var authUser = CreateUserForInvite(emailAddress, language);
        authUser.Projects = [new AuthUserProject(role, projectId)];
        await SendInvitationEmail(authUser, emailAddress, managerName, projectName, language, isProjectInvitation: true);

    }
    private LexAuthUser CreateUserForInvite(string emailAddress, string? language)
    {
        language ??= User.DefaultLocalizationCode;
        return new LexAuthUser
        {
            Id = Guid.NewGuid(),
            Audience = LexboxAudience.RegisterAccount,
            Name = "",
            Email = emailAddress,
            EmailVerificationRequired = null,
            Role = UserRole.user,
            UpdatedDate = DateTimeOffset.Now.ToUnixTimeSeconds(),
            CanCreateProjects = null,
            Locale = language,
            Locked = null,
            Projects = [],
            Orgs = [],
        };
    }
    private async Task SendInvitationEmail(
        LexAuthUser authUser,
        string emailAddress,
        string managerName,
        string resourceName,
        string? language,
        bool isProjectInvitation)
    {
        language ??= User.DefaultLocalizationCode;
        var (jwt, _, lifetime) = lexAuthService.GenerateJwt(authUser, useEmailLifetime: true);
        var email = StartUserEmail(name: "", emailAddress);
        var httpContext = httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(httpContext);

        var queryString = QueryString.Create("email", emailAddress);
        var returnTo = new UriBuilder { Path = "/acceptInvitation", Query = queryString.Value }.Uri.PathAndQuery;
        var registerLink = _linkGenerator.GetUriByAction(httpContext,
            "LoginRedirect",
            "Login",
            new { jwt, returnTo });

        ArgumentException.ThrowIfNullOrEmpty(registerLink);
        if (isProjectInvitation)
        {
            await RenderEmail(email, new ProjectInviteEmail(emailAddress, managerName, resourceName ?? "", registerLink, lifetime), language);
        }
        else
        {
            await RenderEmail(email, new OrgInviteEmail(emailAddress, managerName, resourceName ?? "", registerLink, lifetime), language);
        }
        await SendEmailAsync(email);
    }
    public async Task SendPasswordChangedEmail(User user)
    {
        var email = StartUserEmail(user);
        if (email is null) return;
        await RenderEmail(email, new PasswordChangedEmail(user.Name), user.LocalizationCode);
        await SendEmailWithRetriesAsync(email);
    }

    public async Task SendJoinProjectRequestEmail(User projectManager, User requestingUser, Project project)
    {
        var email = StartUserEmail(projectManager) ?? throw new ArgumentNullException("emailAddress");
        await RenderEmail(email, new JoinProjectRequestEmail(projectManager.Name, requestingUser.Name, requestingUser.Id, project.Code, project.Name), projectManager.LocalizationCode);
        await SendEmailWithRetriesAsync(email);
    }

    public async Task SendCreateProjectRequestEmail(LexAuthUser user, CreateProjectInput projectInput)
    {
        var email = new MimeMessage();
        email.To.Add(new MailboxAddress("Admin", _emailConfig.CreateProjectEmailDestination));
        ArgumentException.ThrowIfNullOrEmpty(user.Email);
        await RenderEmail(email,
            new CreateProjectRequestEmail("Admin", new CreateProjectRequestUser(user.Name, user.Email), projectInput), "en");
        await SendEmailWithRetriesAsync(email);
    }
    public async Task SendApproveProjectRequestEmail(User user, CreateProjectInput projectInput)
    {
        var email = StartUserEmail(user) ?? throw new ArgumentNullException("emailAddress");
        await RenderEmail(email,
            new ApproveProjectRequestEmail(user.Name, new CreateProjectRequestUser(user.Name, user.Email!), projectInput), user.LocalizationCode);
        await SendEmailWithRetriesAsync(email);
    }
    public async Task SendUserAddedEmail(User user, string projectName, string projectCode)
    {
        var email = StartUserEmail(user);
        if (email is null) return; // Guest users have no email address, so we won't notify them by email and that's not an error
        await RenderEmail(email, new UserAddedEmail(user.Name, user.Email!, projectName, projectCode), user.LocalizationCode);
        await SendEmailWithRetriesAsync(email);
    }
    public async Task SendEmailAsync(MimeMessage message)
    {
        message.From.Add(MailboxAddress.Parse(_emailConfig.From));
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag("app.email.subject", message.Subject);
        try
        {
            using var client = new SmtpClient();
            await client.ConnectAsync(_emailConfig.SmtpHost, _emailConfig.SmtpPort);
            activity?.AddEvent(new("connected"));
            await client.AuthenticateAsync(_emailConfig.SmtpUser, _emailConfig.SmtpPassword);
            activity?.AddEvent(new("authenticated"));
            var response = await client.SendAsync(message);
            activity?.AddTag("app.email.smtpResponse", response);
            activity?.AddEvent(new("sent"));
            await client.DisconnectAsync(true);
        }
        catch (Exception e)
        {
            activity?.RecordException(e);
            activity?.SetStatus(ActivityStatusCode.Error);
            throw;
        }
    }
    private async Task SendEmailWithRetriesAsync(MimeMessage message, int retryCount = 3, int retryWaitSeconds = 5 * 60)
    {
        try
        {
            await SendEmailAsync(message);
        }
        catch
        {
            await RetryEmailJob.Queue(schedulerFactory, message, retryCount, retryWaitSeconds);
        }
    }

    private record RenderResult(string Subject, string Html);

    private async Task RenderEmail<T>(MimeMessage message, T parameters, string recipientLocale) where T : EmailTemplateBase
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag("app.email.template", typeof(T).Name);

        var httpClient = clientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("http://" + _emailConfig.EmailRenderHost);
        parameters.BaseUrl = _emailConfig.BaseUrl;
        httpClient.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue(recipientLocale, 1));
        var response = await httpClient.PostAsJsonAsync("email", parameters, jsonSerializerOptions);
        response.EnsureSuccessStatusCode();
        var renderResult = await response.Content.ReadFromJsonAsync<RenderResult>();
        if (renderResult is null)
        {
            activity?.SetStatus(ActivityStatusCode.Error, "RenderResult is null");
            ArgumentNullException.ThrowIfNull(renderResult);
        }

        message.Subject = renderResult.Subject;
        message.Body = new TextPart(TextFormat.Html) { Text = renderResult.Html };
    }

    private static MimeMessage? StartUserEmail(User user, string? email = null)
    {
        var emailAddress = email ?? user.Email;
        if (emailAddress is null) return null;
        return StartUserEmail(user.Name, emailAddress);
    }

    private static MimeMessage StartUserEmail(string name, string email)
    {
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress(name, email));
        return message;
    }
}

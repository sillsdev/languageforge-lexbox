using System.Diagnostics;
using LexBoxApi.Config;
using LexBoxApi.Otel;
using LexBoxApi.Services.Email;
using LexCore.Entities;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using OpenTelemetry.Trace;

namespace LexBoxApi.Services;

public class EmailService
{
    private readonly EmailConfig _emailConfig;
    private readonly IHttpClientFactory _clientFactory;
    private readonly LinkGenerator _linkGenerator;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EmailService(IOptions<EmailConfig> emailConfig,
        IHttpClientFactory clientFactory,
        LinkGenerator linkGenerator,
        IHttpContextAccessor httpContextAccessor
    )
    {
        _clientFactory = clientFactory;
        _linkGenerator = linkGenerator;
        _httpContextAccessor = httpContextAccessor;
        _emailConfig = emailConfig.Value;
    }

    public async Task SendForgotPasswordEmail(string jwt, User user)
    {
        var email = StartUserEmail(user);
        if (!string.IsNullOrEmpty(user.PreviousEmail))
        {
            email.Cc.Add(new MailboxAddress(user.Name, user.PreviousEmail));
        }
        var httpContext = _httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(httpContext);
        // returnTo is a svelte app url
        var forgotLink = _linkGenerator.GetUriByAction(httpContext, "LoginRedirect", "Login", new { jwt, returnTo = "/resetPassword" });
        ArgumentException.ThrowIfNullOrEmpty(forgotLink);
        await RenderEmail(email, new ForgotPasswordEmail(user.Name, forgotLink));
        await SendEmailAsync(email);
    }

    public async Task SendVerifyAddressEmail(string jwt, User user)
    {
        ArgumentException.ThrowIfNullOrEmpty(user.EmailVerificationToken);
        var email = StartUserEmail(user);
        var httpContext = _httpContextAccessor.HttpContext;
        ArgumentNullException.ThrowIfNull(httpContext);
        var verifyLink = _linkGenerator.GetUriByAction(httpContext, "VerifyEmail", "Login", new
        {
            jwt,
            returnTo = "/user?verifiedEmail",
            token = user.EmailVerificationToken,
            email = user.Email,
        });
        ArgumentException.ThrowIfNullOrEmpty(verifyLink);
        await RenderEmail(email, new VerifyAddressEmail(user.Name, verifyLink));
        await SendEmailAsync(email);
    }

    public async Task SendPasswordChangedEmail(User user)
    {
        var email = StartUserEmail(user);
        await RenderEmail(email, new PasswordChangedEmail(user.Name));
        await SendEmailAsync(email);
    }

    private async Task SendEmailAsync(MimeMessage message)
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

    private record RenderResult(string Subject, string Html);

    private async Task RenderEmail<T>(MimeMessage message, T parameters) where T : EmailTemplateBase
    {
        using var activity = LexBoxActivitySource.Get().StartActivity();
        activity?.AddTag("app.email.template", typeof(T).Name);

        var httpClient = _clientFactory.CreateClient();
        httpClient.BaseAddress = new Uri("http://" + _emailConfig.EmailRenderHost);
        var response = await httpClient.PostAsJsonAsync("email", parameters);
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

    private static MimeMessage StartUserEmail(User user)
    {
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress(user.Name, user.Email));
        return message;
    }
}

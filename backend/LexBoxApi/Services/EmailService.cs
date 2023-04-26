using System.Diagnostics;
using LexBoxApi.Config;
using LexBoxApi.Otel;
using LexBoxApi.Services.Email;
using LexData;
using MailKit.Net.Smtp;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;
using MimeKit.Text;
using OpenTelemetry.Trace;

namespace LexBoxApi.Services;

public class EmailService
{
    private readonly EmailConfig _emailConfig;
    private readonly LexBoxDbContext _dbContext;
    private readonly IHttpClientFactory _clientFactory;

    public EmailService(IOptions<EmailConfig> emailConfig, LexBoxDbContext dbContext, IHttpClientFactory clientFactory)
    {
        _dbContext = dbContext;
        _clientFactory = clientFactory;
        _emailConfig = emailConfig.Value;
    }

    public async Task<bool> SendForgotPasswordEmail(string email)
    {
        var user = await _dbContext.Users.SingleOrDefaultAsync(u => u.Email == email);
        // we want to silently return if the user doesn't exist, so we don't leak information.
        if (user is null) return false;
        var message = new MimeMessage();
        message.To.Add(new MailboxAddress(user.Name, user.Email));
        // todo generate jwt for password reset.
        await RenderEmail(message, new ForgotPasswordEmail(user.Name, "https://lexbox.org"));
        await SendEmailAsync(message);
        return true;
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
            await client.SendAsync(message);
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
}
using LexBoxApi.Auth;
using LexCore.Analytics;
using LexCore.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

/// <summary>
/// LexBox implementation of <see cref="ILexboxAnalyticsService"/>. Resolves the current user from the
/// request and injects it into each event, then sends via the shared <see cref="MixpanelClient"/>.
/// </summary>
public class LexboxAnalyticsService(
    MixpanelClient mixpanelClient,
    IOptions<AnalyticsConfigBase> analyticsConfig,
    IHostEnvironment environment,
    LoggedInContext loggedInContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider? timeProvider = null) : ILexboxAnalyticsService
{
    // Cloudflare sets the original visitor IP here; lexbox sits behind it in production.
    private const string CloudflareClientIpHeader = "CF-Connecting-IP";
    private const string ForwardedForHeader = "X-Forwarded-For";

    private readonly AnalyticsConfigBase _config = analyticsConfig.Value;
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public Task TrackSendReceiveCompleted()
    {
        var user = loggedInContext.MaybeUser;
        if (user?.Id is not { } userId || userId == Guid.Empty)
            return Task.CompletedTask;
        // Respect the user's opt-out (carried on the JWT claim, re-issued whenever they change it).
        if (user.OptedOutOfAnalytics == true)
            return Task.CompletedTask;
        var properties = CreateBaseProperties();
        if (properties is null)
            return Task.CompletedTask;
        return Task.Run(() => mixpanelClient.SendAsync(ILexboxAnalyticsService.SendReceiveCompletedEvent, properties));
    }

    public Task TrackLoginCompleted(LexAuthUser user, string loginType)
    {
        if (user.Id == Guid.Empty)
            return Task.CompletedTask;
        // Respect the user's opt-out (carried on the JWT claim, re-issued whenever they change it).
        if (user.OptedOutOfAnalytics == true)
            return Task.CompletedTask;
        var properties = CreateBaseProperties(user.Id);
        if (properties is null)
            return Task.CompletedTask;
        properties[ILexboxAnalyticsService.LoginTypeProperty] = loginType;
        return Task.Run(() => mixpanelClient.SendAsync(ILexboxAnalyticsService.LoginCompletedEvent, properties));
    }

    public Task TrackAccountCreated(Guid userId, AccountCreatedVia createdVia)
    {
        if (userId == Guid.Empty)
            return Task.CompletedTask;
        var properties = CreateBaseProperties(userId);
        if (properties is null)
            return Task.CompletedTask;
        properties[ILexboxAnalyticsService.CreatedViaProperty] = createdVia.ToMixpanelValue();
        return Task.Run(() => mixpanelClient.SendAsync(ILexboxAnalyticsService.AccountCreatedEvent, properties));
    }

    /// <summary>
    /// Base Mixpanel properties, or null when analytics should not send at all.
    /// Falls back to the current request's user for <c>$user_id</c> when one is not supplied
    /// (login events pass it explicitly since the request itself is still anonymous).
    /// </summary>
    private Dictionary<string, object?>? CreateBaseProperties(Guid? userId = null)
    {
        if (!_config.Enabled)
            return null;
        var token = MixpanelTokens.SelectToken(
            environment.IsDevelopment(),
            _config.DebugProjectToken,
            _config.ProductionToken);
        if (token is null)
            return null;
        var properties = MixpanelProperties.CreateBase(
            token,
            _config.Product,
            _clock.GetUtcNow(),
            Guid.NewGuid().ToString());
        // Stamp the app version on every lexbox event (matches FwLite's $app_version_string).
        properties["$app_version_string"] = AppVersionService.Version;
        // Server-side: supply the end user's IP so Mixpanel geolocates them, not the lexbox server.
        // SendAsync is called without ip=1, so Mixpanel uses this property instead of the request IP.
        if (GetClientIp() is { } clientIp)
            properties["ip"] = clientIp;
        userId ??= loggedInContext.MaybeUser?.Id;
        if (userId is Guid id && id != Guid.Empty)
            properties["$user_id"] = id.ToString();
        return properties;
    }

    /// <summary>
    /// The end user's IP address for this request: Cloudflare's <c>CF-Connecting-IP</c> in production,
    /// otherwise the first <c>X-Forwarded-For</c> hop, otherwise the socket peer. Null when there is no
    /// request (e.g. a background sync) or no address can be determined.
    /// </summary>
    private string? GetClientIp()
    {
        var request = httpContextAccessor.HttpContext?.Request;
        if (request is null)
            return null;

        var cloudflareIp = request.Headers[CloudflareClientIpHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(cloudflareIp))
            return cloudflareIp.Trim();

        var forwardedFor = request.Headers[ForwardedForHeader].FirstOrDefault();
        if (!string.IsNullOrWhiteSpace(forwardedFor))
            return forwardedFor.Split(',')[0].Trim();

        return httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
    }
}

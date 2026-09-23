using LexBoxApi.Auth;
using LexCore.Analytics;
using LexCore.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace LexBoxApi.Services;

/// <summary>
/// Resolves the current user from the request and injects it into each event, then sends via the shared
/// <see cref="MixpanelClient"/>.
/// </summary>
public class LexboxAnalyticsService(
    MixpanelClient mixpanelClient,
    IOptions<AnalyticsConfigBase> analyticsConfig,
    IHostEnvironment environment,
    LoggedInContext loggedInContext,
    IHttpContextAccessor httpContextAccessor,
    TimeProvider? timeProvider = null) : ILexboxAnalyticsService
{
    private readonly AnalyticsConfigBase _config = analyticsConfig.Value;
    private readonly TimeProvider _clock = timeProvider ?? TimeProvider.System;

    public Task TrackSendReceive(string direction) =>
        TrackCurrentUserEvent(ILexboxAnalyticsService.SendReceiveEvent,
            new() { [ILexboxAnalyticsService.DirectionProperty] = direction });

    public Task TrackFwLiteSync() => TrackCurrentUserEvent(ILexboxAnalyticsService.FwLiteSyncEvent);

    /// <summary>
    /// Send <paramref name="eventName"/> for the currently signed-in user, with any <paramref name="extraProperties"/>
    /// merged onto the base properties. Sends nothing when there is no identified user (e.g. an automated
    /// service-account sync) or they have opted out.
    /// </summary>
    private Task TrackCurrentUserEvent(string eventName, Dictionary<string, object?>? extraProperties = null)
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
        if (extraProperties is not null)
        {
            foreach (var (key, value) in extraProperties)
                properties[key] = value;
        }
        return Task.Run(() => mixpanelClient.SendAsync(eventName, properties));
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

    public Task TrackAccountCreated(Guid userId, AccountCreatedVia createdVia, bool optedOutOfAnalytics = false)
    {
        if (userId == Guid.Empty)
            return Task.CompletedTask;
        // Respect an opt-out the user made while creating the account (register/invitation consent checkbox).
        if (optedOutOfAnalytics)
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
    /// The end user's IP address for this request. Resolution to the real visitor (Cloudflare/forwarded
    /// headers) happens in middleware, so this just reads the connection's remote IP. Null when there is no
    /// request (e.g. a background sync).
    /// </summary>
    private string? GetClientIp() =>
        httpContextAccessor.HttpContext?.Connection.RemoteIpAddress?.ToString();
}

using LexBoxApi.Auth;
using LexCore.Analytics;
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
    TimeProvider? timeProvider = null) : ILexboxAnalyticsService
{
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

    /// <summary>Base Mixpanel properties, or null when analytics should not send at all.</summary>
    private Dictionary<string, object?>? CreateBaseProperties()
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
        if (loggedInContext.MaybeUser?.Id is Guid userId && userId != Guid.Empty)
            properties["$user_id"] = userId.ToString();
        return properties;
    }
}

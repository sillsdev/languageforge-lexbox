namespace LexCore.Analytics;

/// <summary>
/// How a user account came to exist, reported as the <c>created_via</c> property on the
/// <see cref="ILexboxAnalyticsService.AccountCreatedEvent"/> event.
/// </summary>
public enum AccountCreatedVia
{
    /// <summary>Self-service registration via the register form.</summary>
    Registration,

    /// <summary>The user accepted an email invitation and set their password.</summary>
    Invitation,

    /// <summary>An admin created a guest account on the user's behalf.</summary>
    Admin,

    /// <summary>A manager added a not-yet-registered email/username to a project.</summary>
    ProjectInvite,
}

public static class AccountCreatedViaExtensions
{
    /// <summary>The stable snake_case value sent to Mixpanel for this creation source.</summary>
    public static string ToMixpanelValue(this AccountCreatedVia via) => via switch
    {
        AccountCreatedVia.Registration => "registration",
        AccountCreatedVia.Invitation => "invitation",
        AccountCreatedVia.Admin => "admin",
        AccountCreatedVia.ProjectInvite => "project_invite",
        _ => throw new ArgumentOutOfRangeException(nameof(via), via, null),
    };
}

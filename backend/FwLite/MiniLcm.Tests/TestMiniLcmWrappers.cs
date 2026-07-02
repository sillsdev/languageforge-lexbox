using Microsoft.Extensions.DependencyInjection;
using MiniLcm.Validators;
using MiniLcm.Wrappers;

namespace MiniLcm.Tests;

/// <summary>
/// Builds the production MiniLcm wrapper stack via <see cref="MiniLcmValidatorsExtensions.AddMiniLcmValidators"/>,
/// so tests exercise the same validators and wrapper composition that real API entry points use
/// (and pick up new registrations automatically). The resolved wrappers hold no provider-scoped state,
/// so the throwaway provider is disposed immediately.
/// </summary>
public static class TestMiniLcmWrappers
{
    private static ServiceProvider BuildProvider() =>
        new ServiceCollection().AddMiniLcmValidators().BuildServiceProvider();

    /// <summary>
    /// The full user-facing stack (query normalization, validation, write normalization) that real
    /// API entry points apply via <see cref="MiniLcmApiUserFacingWrappers"/>.
    /// </summary>
    public static MiniLcmApiUserFacingWrappers CreateUserFacingWrappers()
    {
        using var provider = BuildProvider();
        return provider.GetRequiredService<MiniLcmApiUserFacingWrappers>();
    }

    /// <summary>
    /// The validation-only wrapper that the sync path applies (see <c>CrdtFwdataProjectSyncService</c>),
    /// which deliberately skips normalization because both sides are already normalized.
    /// </summary>
    public static MiniLcmApiValidationWrapperFactory CreateValidationFactory()
    {
        using var provider = BuildProvider();
        return provider.GetRequiredService<MiniLcmApiValidationWrapperFactory>();
    }
}

using MiniLcm.Normalization;
using MiniLcm.Validators;

namespace MiniLcm.Wrappers;

/// <summary>
/// The standard wrapper stack applied at every user-facing API entry point:
/// MiniLcmJsInvokable, MiniLcmApiHubBase, and MiniLcmRoutes.
///
/// Write normalisation is applied uniformly to both CRDT and FwData rather than
/// conditionally. The invariant "always normalise at the user-facing boundary" is simpler
/// to reason about than backend-specific rules. For FwData, LibLCM normalises internally
/// anyway, so the wrapper is effectively a no-op; FwData is desktop-only, making the
/// redundant pass inconsequential.
/// </summary>
public class MiniLcmApiUserFacingWrappers(
    MiniLcmApiStringNormalizationWrapperFactory readNormalization,
    MiniLcmWriteApiNormalizationWrapperFactory writeNormalization,
    MiniLcmApiValidationWrapperFactory validation)
{
    /// <param name="innerWrappers">
    /// Additional factories appended after validation (i.e. applied innermost, closest to the raw API).
    /// </param>
    public IMiniLcmApi Apply(IMiniLcmApi api, IProjectIdentifier project, params IMiniLcmWrapperFactory?[] innerWrappers)
    {
        return api.WrapWith([readNormalization, writeNormalization, validation, ..innerWrappers], project);
    }
}

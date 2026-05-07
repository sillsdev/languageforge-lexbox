using MiniLcm.Normalization;
using MiniLcm.Validators;

namespace MiniLcm.Wrappers;

/// <summary>
/// The standard wrapper stack applied at every user-facing API entry point:
/// MiniLcmJsInvokable, MiniLcmApiHubBase, and MiniLcmRoutes.
///
/// Call order: read normalisation → validation → write normalisation → raw API.
/// Validation runs before write normalisation so that bad input is rejected with a
/// meaningful error before the normaliser sees it; the normaliser can therefore assume
/// structurally valid data. Write normalisation is applied uniformly to both CRDT and
/// FwData: for CRDT it ensures NFD storage; for FwData, LibLCM also normalises
/// internally, but the uniform approach avoids backend-specific reasoning. FwData is
/// desktop-only, so the redundant pass is inconsequential.
/// The wrapper creates new instances of every normalised object, so the caller's
/// original is never mutated and the inner API always receives a clean copy.
/// </summary>
public class MiniLcmApiUserFacingWrappers(
    MiniLcmApiQueryNormalizationWrapperFactory readNormalization,
    MiniLcmApiWriteNormalizationWrapperFactory writeNormalization,
    MiniLcmApiValidationWrapperFactory validation)
{
    /// <param name="innerWrappers">
    /// Additional factories appended after write normalisation (i.e. applied innermost, closest to the raw API).
    /// </param>
    public IMiniLcmApi Apply(IMiniLcmApi api, IProjectIdentifier project, params IMiniLcmWrapperFactory?[] innerWrappers)
    {
        // Validation before write normalisation: bad input is rejected with a clear error
        // before the normaliser sees it, so the normaliser can assume structurally valid data.
        return api.WrapWith([readNormalization, validation, writeNormalization, ..innerWrappers], project);
    }
}

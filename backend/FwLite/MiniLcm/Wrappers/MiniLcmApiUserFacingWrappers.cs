using MiniLcm.Normalization;
using MiniLcm.Validators;

namespace MiniLcm.Wrappers;

/// <summary>
/// The standard wrapper stack applied at every user-facing API entry point:
/// MiniLcmJsInvokable, MiniLcmApiHubBase, and MiniLcmRoutes.
///
/// Write normalisation is applied uniformly to both CRDT and FwData. For CRDT it ensures
/// NFD storage. For FwData, LibLCM already normalises, but uniform application also covers
/// objects created transitively by Update* calls — senses or example sentences produced by
/// the internal diff-and-sync logic during UpdateEntry, for example — which would otherwise
/// escape normalisation if the wrapper were skipped for FwData. FwData is desktop-only,
/// so the redundant pass is inconsequential.
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

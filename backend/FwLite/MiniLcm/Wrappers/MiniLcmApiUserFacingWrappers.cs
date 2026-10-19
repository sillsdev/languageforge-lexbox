using MiniLcm.Models;
using MiniLcm.Normalization;
using MiniLcm.Validators;

namespace MiniLcm.Wrappers;

/// <summary>
/// The standard wrapper stack applied at every user-facing API entry point:
/// MiniLcmJsInvokable and MiniLcmRoutes.
/// </summary>
public class MiniLcmApiUserFacingWrappers(
    MiniLcmApiQueryNormalizationWrapperFactory readNormalization,
    MiniLcmApiWriteNormalizationWrapperFactory writeNormalization,
    MiniLcmApiValidationWrapperFactory validation)
{
    public IMiniLcmApi Apply(IMiniLcmApi api, IProjectIdentifier project, params IMiniLcmWrapperFactory[] innerWrappers)
    {
        // Validation before write normalisation: bad input is rejected with a clear error
        // before the normaliser sees it, so the normaliser can assume structurally valid data.
        // Write normalisation is applied uniformly to both CRDT and FwData:
        // It's redundant for FwData (because LibLCM also normalises internally),
        // but the uniformity is simpler and the normaliser creates new instances when it normalises.
        // We want that behaviour to always be in place for simplicity.
        // FwData is desktop-only, so the redundant pass is inconsequential cost-wise.
        return api.WrapWith([readNormalization, validation, writeNormalization, .. innerWrappers], project);
    }
}

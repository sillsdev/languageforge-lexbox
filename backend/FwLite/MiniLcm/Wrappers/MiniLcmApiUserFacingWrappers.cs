using MiniLcm.Normalization;
using MiniLcm.Validators;

namespace MiniLcm.Wrappers;

/// <summary>
/// The standard wrapper stack applied at every user-facing API entry point
/// (MiniLcmJsInvokable, MiniLcmApiHubBase, MiniLcmRoutes).
///
/// Write normalisation is applied to both CRDT and FwData backends. For CRDT it ensures
/// data enters the store in NFD. For FwData, LibLCM already normalises internally — but
/// the wrapper also enforces the <c>api ?? this</c> self-reference contract on Update*
/// overloads, giving consistent behaviour across backends. FwData is desktop-only, so
/// the negligible cost of a redundant normalisation pass is of no concern.
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

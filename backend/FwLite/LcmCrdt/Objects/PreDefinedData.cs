using LcmCrdt.Changes;
using SIL.Harmony;

namespace LcmCrdt.Objects;

public static class PreDefinedData
{
    internal static async Task AddPredefinedMorphTypes(DataModel dataModel, ProjectData projectData)
    {
        await dataModel.AddChanges(projectData.ClientId,
            [.. CanonicalMorphTypes.All.Values.Select(mt => new CreateMorphTypeChange(mt))]);
    }
}

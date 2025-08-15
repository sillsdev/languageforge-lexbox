using SIL.LCModel;

namespace FwDataMiniLcmBridge.Api;

public static class PossibilityExtensions
{
    public static IEnumerable<T> Flatten<T>(this IEnumerable<T> enumerable) where T : ICmPossibility
    {
        foreach (var cmPossibility in enumerable)
        {
            yield return cmPossibility;
            foreach (var child in Flatten(cmPossibility.SubPossibilitiesOS.Cast<T>()))
            {
                yield return child;
            }
        }
    }
}

using FluentAssertions.Equivalency;
using MiniLcm.Models;

namespace MiniLcm.Tests.Helpers;

public static class FluentAssertHelpers
{
    public static EquivalencyAssertionOptions<T> ExcludingVersion<T>(this EquivalencyAssertionOptions<T> options) where T: IObjectWithId
    {
        return options.Excluding(bool (info) => info.Name == "Version");
    }
}

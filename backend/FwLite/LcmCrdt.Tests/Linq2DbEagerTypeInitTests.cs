using System.Runtime.CompilerServices;
using LinqToDB.EntityFrameworkCore;

namespace LcmCrdt.Tests;

public class Linq2DbEagerTypeInitTests
{
    // linq2db.EntityFrameworkCore 10.3.x/10.4.x shipped a SqlTransparentExpression cctor that threw,
    // crashing FwLiteMaui on Android (Mono initializes types eagerly; desktop never ran the cctor).
    // Guards future package bumps against a regression of https://github.com/sillsdev/languageforge-lexbox/issues/2291.
    [Fact]
    public void SqlTransparentExpressionCctorDoesNotThrow()
    {
        var type = typeof(LinqToDBForEFTools).Assembly
            .GetType("LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression");
        type.Should().NotBeNull();
        var act = () => RuntimeHelpers.RunClassConstructor(type.TypeHandle);
        act.Should().NotThrow();
    }
}

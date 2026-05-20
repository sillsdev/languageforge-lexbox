using System;
using System.Runtime.CompilerServices;

namespace LcmCrdt.Tests;

// Empirical repro for the linq2db.EntityFrameworkCore 10.3.x bug where
// EFCoreMetadataReader+SqlTransparentExpression's static ctor throws because
// GetConstructor looks up an (ExceptExpression, RelationalTypeMapping) signature
// that doesn't exist on the type. See FwLiteMaui.csproj's cctor-patcher target
// and backend/FwLite/LcmCrdt/LINQ2DB-V6-NOTES.md (Cctor patcher section).
//
// IMPORTANT: These tests intentionally still FAIL on desktop. They probe the
// in-process linq2db.EntityFrameworkCore.dll loaded from NuGet — which is the
// shipping-broken assembly. The cctor patcher only rewrites the *Android-staged*
// copy in $(IntermediateOutputPath); the desktop test process loads the
// un-patched NuGet one. Failing here proves the upstream bug still exists; the
// Android binary check is what proves the patch is wired correctly. So these
// are marked Skip on .NET Core / desktop runs to keep the suite green, while
// the Cecil-disassembly check elsewhere is the load-bearing assertion.
//
// UNSKIP WHEN: the linq2db.EntityFrameworkCore version pin in
// FwLiteMaui.csproj (_VerifyLinq2DbEfCoreVersionPin) is bumped or removed.
// At that point either:
//   - the bug is fixed upstream → these tests should pass without any patcher;
//     unskip them, delete the patcher, and they become a permanent regression
//     guard.
//   - the bug still exists in the new version → run unskipped against the new
//     version to confirm the same repro shape, then re-skip with an updated
//     reason and widen the version pin.
public class SqlTransparentExpressionCctorRepro
{
    private const string SkipReason =
        "Probes the unpatched NuGet linq2db.EntityFrameworkCore.dll loaded in the test " +
        "process — repro only, not a regression test. The Android build's cctor patcher " +
        "operates on the staged dll under obj/<Config>/net10.0-android/, not on the dll " +
        "loaded here. Verify the Android patch by Cecil-inspecting that staged dll.";

    [Fact(Skip = SkipReason)]
    public void Cctor_runs_without_throwing()
    {
        var t = Type.GetType(
            "LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression, linq2db.EntityFrameworkCore",
            throwOnError: true)!;
        var act = () => RuntimeHelpers.RunClassConstructor(t.TypeHandle);
        act.Should().NotThrow();
    }

    [Fact(Skip = SkipReason)]
    public void Cctor_is_stubbed_via_field_check()
    {
        var t = Type.GetType(
            "LinqToDB.EntityFrameworkCore.EFCoreMetadataReader+SqlTransparentExpression, linq2db.EntityFrameworkCore",
            throwOnError: true)!;
        var f = t.GetField("_ctor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var act = () => f.GetValue(null);
        act.Should().NotThrow();
    }
}

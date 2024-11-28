using Chorus.VcsDrivers.Mercurial;
using SIL.CommandLineProcessing;
using SIL.PlatformUtilities;
using SIL.Progress;

namespace FwLiteProjectSync.Tests.Fixtures;

public static class MercurialTestHelper
{
    private static readonly NullProgress NullProgress = new NullProgress();

    private static string RunHgCommand(string repoPath, string args)
    {
        var result = HgRunner.Run(args, repoPath, 120, NullProgress);
        if (result.ExitCode == 0) return result.StandardOutput;
        throw new Exception(
            $"hg {args} failed.\nStdOut: {result.StandardOutput}\nStdErr: {result.StandardError}");

    }

    public static void HgClean(string repoPath, string exclude)
    {
        RunHgCommand(repoPath, $"purge --no-confirm --exclude {exclude}");
    }

    public static void HgUpdate(string repoPath, string rev)
    {
        RunHgCommand(repoPath, $"update \"{rev}\"");
    }
}


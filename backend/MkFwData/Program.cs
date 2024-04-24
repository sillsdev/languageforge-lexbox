using Chorus.VcsDrivers.Mercurial;
using SIL.Progress;
using System.CommandLine;

class Program
{
    static async Task Main(string[] args)
    {
        var rootCommand = new RootCommand("Make .fwdata file");

        var verboseOption = new Option<bool>(
            ["--verbose", "-v"],
            "Display verbose output"
        );
        rootCommand.AddGlobalOption(verboseOption);

        var quietOption = new Option<bool>(
            ["--quiet", "-q"],
            "Suppress all output (overrides --verbose if present)"
        );
        rootCommand.AddGlobalOption(quietOption);

        var file = new Argument<FileSystemInfo>(
            "file",
            "Name of .fwdata file to create"
        );
        rootCommand.Add(file);

        var hgRevOption = new Option<string>(
            ["--rev", "-r"],
            "Revision to check out (default \"tip\")"
        );
        hgRevOption.SetDefaultValue("tip");
        rootCommand.Add(hgRevOption);

        rootCommand.SetHandler(Run, file, verboseOption, quietOption, hgRevOption);

        await rootCommand.InvokeAsync(args);
    }

    static void Run(FileSystemInfo file, bool verbose, bool quiet, string rev)
    {
        IProgress progress = quiet ? new NullProgress() : new ConsoleProgress();
        progress.ShowVerbose = verbose;
        bool isDir = file.Exists && (file.Attributes & FileAttributes.Directory) != 0;
        string name = isDir ? Path.Join(file.FullName, file.Name + ".fwdata") : file.FullName;
        string dir = isDir ? file.FullName : new FileInfo(file.FullName).Directory!.FullName;
        HgRunner.Run($"hg checkout {rev}", dir, 30, progress);
        progress.WriteVerbose("Creating {0} ...", name);
        LfMergeBridge.LfMergeBridge.ReassembleFwdataFile(progress, writeVerbose: true, name);
        progress.WriteMessage("Created {0}", name);
    }
}

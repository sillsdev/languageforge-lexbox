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

        rootCommand.SetHandler(Run, file, verboseOption, quietOption);

        await rootCommand.InvokeAsync(args);
    }

    static void Run(FileSystemInfo file, bool verbose, bool quiet)
    {
        IProgress progress = quiet ? new NullProgress() : new ConsoleProgress();
        progress.ShowVerbose = verbose;
        bool isDir = file.Exists && (file.Attributes & FileAttributes.Directory) != 0;
        string name = isDir ? Path.Join(file.FullName, file.Name + ".fwdata") : file.FullName;
        progress.WriteVerbose("Creating {0} ...", name);
        LfMergeBridge.LfMergeBridge.PutHumptyTogetherAgain(progress, writeVerbose: true, name);
        progress.WriteMessage("Created {0}", name);
    }
}

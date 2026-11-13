using System.Diagnostics;
using System.Text.Json;
using Chorus;
using Chorus.Model;
using Chorus.VcsDrivers.Mercurial;
using Nini.Ini;
using SIL.Progress;
using SendReceiveClient;

// Usage: SendReceiveClient <version|lfmergebridge|hg>, with a ClientRequest as JSON on stdin.
// Writes a ClientResponse as JSON to stdout; progress goes to stderr as it happens.
var stdout = Console.Out;
Console.SetOut(Console.Error);
// LfMergeBridge runs FixFwData from the current directory
Directory.SetCurrentDirectory(Path.Join(AppContext.BaseDirectory, "FixFwDataTool"));

ClientResponse response;
try
{
    var input = Console.In.ReadToEnd();
    var request = string.IsNullOrWhiteSpace(input) ? new ClientRequest() : JsonSerializer.Deserialize<ClientRequest>(input)!;
    response = args[0] switch
    {
        "version" => Version(),
        "lfmergebridge" => RunLfMergeBridge(request),
        "hg" => RunHg(request),
        _ => throw new ArgumentException($"Unknown command '{args[0]}'")
    };
}
catch (Exception e)
{
    response = new ClientResponse("", e.GetType().Name, e.ToString());
}

stdout.WriteLine(JsonSerializer.Serialize(response));
return 0;

static ClientResponse Version()
{
    var hgVersion = HgRunner.Run("hg version", Environment.CurrentDirectory, 5, new StderrProgress()).StandardOutput;
    var output = $"""
                  {hgVersion}
                  LibChorus {VersionOf(typeof(HgRepository))}, LfMergeBridge {VersionOf(typeof(LfMergeBridge.LfMergeBridge))}
                  """;
    var readiness = HgRepository.GetEnvironmentReadinessMessage("en");
    return readiness is null
        ? new ClientResponse(output)
        : new ClientResponse(output, "EnvironmentNotReady", readiness);
}

static string? VersionOf(Type type) => FileVersionInfo.GetVersionInfo(type.Assembly.Location).ProductVersion?.Split('+')[0];

static ClientResponse RunLfMergeBridge(ClientRequest request)
{
    FixupCaCerts();
    if (!string.IsNullOrEmpty(request.Username) || !string.IsNullOrEmpty(request.Password))
    {
        new ServerSettingsModel
        {
            Username = request.Username,
            RememberPassword = false, // Necessary for tests to work on Linux
            Password = request.Password
        }.SaveUserSettings();
    }

    var progress = new StderrProgress();
    LfMergeBridge.LfMergeBridge.Execute(request.Action, progress, request.Options, out var result);
    return new ClientResponse($"{result}{Environment.NewLine}Progress out: {progress.Text}");
}

static ClientResponse RunHg(ClientRequest request)
{
    var result = HgRunner.Run($"hg {request.HgArgs}", request.Dir, 5, new StderrProgress());
    return result.ExitCode == 0
        ? new ClientResponse(result.StandardOutput)
        : new ClientResponse(result.StandardOutput, "HgFailed", $"hg {request.HgArgs} exited with {result.ExitCode}: {result.StandardError}");
}

static void FixupCaCerts()
{
    var caCertsPem = new[]
    {
        "/etc/ssl/certs/ca-certificates.crt",
        Path.GetFullPath(Path.Join(MercurialLocation.PathToMercurialFolder, "cacert.pem")),
    }.FirstOrDefault(File.Exists) ?? throw new FileNotFoundException("unable to find cacert.pem");
    //this cacerts.rc file is what is used when doing a clone, all future actions on a repo use the hgrc file defined in the .hg folder
    var cacertsRcPath = Path.Join(MercurialLocation.PathToMercurialFolder, "default.d", "cacerts.rc");
    //the default.d folder doesn't exist in linux builds, so we modify the mercurial.ini file instead
    if (!File.Exists(cacertsRcPath))
    {
        cacertsRcPath = Path.Join(MercurialLocation.PathToMercurialFolder, "mercurial.ini");
    }

    var caCertsRc = new IniDocument(cacertsRcPath, IniFileType.MercurialStyle);
    caCertsRc.Sections.GetOrCreate("web").Set("cacerts", caCertsPem);
    caCertsRc.Save();
}

namespace SendReceiveClient
{
    internal class StderrProgress : StringBuilderProgress
    {
        public StderrProgress()
        {
            ProgressIndicator = new NullProgressIndicator();
            ShowVerbose = true;
        }

        public override void WriteMessage(string message, params object[] args)
        {
            // Chorus logs things like 'log -r0 --template "{node}"', so only format when args were actually passed
            Console.Error.WriteLine(args is { Length: > 0 } ? string.Format(message, args) : message);
            base.WriteMessage(message, args);
        }
    }
}

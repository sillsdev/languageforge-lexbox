using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using FluentAssertions;
using SendReceiveClient;
using Xunit.Abstractions;

namespace Testing.Services;

public record SendReceiveAuth(string Username, string Password);

/// <summary>
/// An exception SendReceiveClient caught. Its type lives in the client's LibChorus, so only the name crosses over.
/// </summary>
public class SendReceiveClientException(string exceptionType, string? details) : Exception($"{exceptionType}: {details}")
{
    public string ExceptionType { get; } = exceptionType;
}

public record SendReceiveParams(string ProjectCode, string BaseUrl, string Dir) : ProjectPath(ProjectCode, Dir)
{
    public SendReceiveParams(HgProtocol protocol, ProjectPath project) : this(project.Code, protocol.GetTestHostName(), project.Dir) { }
}

public class SendReceiveService(ITestOutputHelper output)
{
    private const string fdoDataModelVersion = "7000072";

    private static readonly string ClientPath = typeof(SendReceiveService).Assembly
        .GetCustomAttributes<AssemblyMetadataAttribute>()
        .Single(a => a.Key == "SendReceiveClientPath").Value!;

    /// <summary>
    /// hg version, LibChorus and LfMergeBridge versions of the client. Throws if Chorus says hg isn't ready to use.
    /// </summary>
    public static string GetHgVersion() => RunClient("version", new ClientRequest(), null);

    public string RunHg(string args, string dir) => RunClient("hg", new ClientRequest(HgArgs: args, Dir: dir), output);

    private static string RunClient(string command, ClientRequest request, ITestOutputHelper? output)
    {
        using var client = new Process();
        client.StartInfo = new ProcessStartInfo("dotnet", [ClientPath, command])
        {
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        client.ErrorDataReceived += (_, e) =>
        {
            if (e.Data is not null) output?.WriteLine(e.Data);
        };
        client.Start();
        client.BeginErrorReadLine();
        client.StandardInput.Write(JsonSerializer.Serialize(request));
        client.StandardInput.Close();
        var stdout = client.StandardOutput.ReadToEnd();
        client.WaitForExit();

        var response = JsonSerializer.Deserialize<ClientResponse>(stdout)
                       ?? throw new InvalidOperationException($"SendReceiveClient wrote no response: {stdout}");
        if (response.ExceptionType is not null)
            throw new SendReceiveClientException(response.ExceptionType, response.Exception);
        return response.Output;
    }

    public string RunCloneSendReceive(SendReceiveParams sendReceiveParams, SendReceiveAuth auth)
    {
        var projectDir = sendReceiveParams.Dir;
        var fwDataFile = sendReceiveParams.FwDataFile;

        // Clone
        var cloneResult = CloneProject(sendReceiveParams, auth);

        Directory.Exists(projectDir).Should().BeTrue($"Directory {projectDir} not found. Clone response: {cloneResult}");
        Directory.EnumerateFiles(projectDir).Should().Contain(fwDataFile);
        var fwDataFileInfo = new FileInfo(fwDataFile);
        fwDataFileInfo.Length.Should().BeGreaterThan(0);
        var fwDataFileOriginalLength = fwDataFileInfo.Length;

        // SendReceive
        var srResult = SendReceiveProject(sendReceiveParams, auth);

        srResult.Should().Contain("No changes from others");
        fwDataFileInfo.Refresh();
        fwDataFileInfo.Exists.Should().BeTrue();
        fwDataFileInfo.Length.Should().Be(fwDataFileOriginalLength);

        return $"Clone: {cloneResult}{Environment.NewLine}SendReceive: {srResult}";
    }

    public string CloneProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth, bool validateOutput = true)
    {
        var (projectCode, baseUrl, destDir) = sendReceiveParams;
        var (username, password) = auth;
        var repoUrl = new UriBuilder($"{TestingEnvironmentVariables.HttpScheme}{baseUrl}/{projectCode}");
        output.WriteLine($"Cloning {repoUrl} with user '{username}' ...");
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
        {
            repoUrl.UserName = username;
            repoUrl.Password = password;
        }

        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "deleteRepoIfNoSuchBranch", "false" },
        };
        var cloneResult = RunClient("lfmergebridge", new ClientRequest("Language_Forge_Clone", flexBridgeOptions, username, password), output);

        if (validateOutput)
        {
            Utils.ValidateSendReceiveOutput(cloneResult);
        }

        return cloneResult;
    }

    public string SendReceiveProject(SendReceiveParams sendReceiveParams, SendReceiveAuth auth, string commitMessage = "Testing", bool validateOutput = true)
    {
        var (projectCode, baseUrl, destDir) = sendReceiveParams;
        var (username, password) = auth;
        var repoUrl = new UriBuilder($"{TestingEnvironmentVariables.HttpScheme}{baseUrl}/{projectCode}");
        output.WriteLine($"S/R for {repoUrl} with user '{username}' ...");
        if (!string.IsNullOrEmpty(username) || !string.IsNullOrEmpty(password))
        {
            repoUrl.UserName = username;
            repoUrl.Password = password;
        }

        string fwdataFilename = Path.Join(destDir, $"{projectCode}.fwdata");
        var flexBridgeOptions = new Dictionary<string, string>
        {
            { "fullPathToProject", destDir },
            { "fwdataFilename", fwdataFilename },
            { "fdoDataModelVersion", fdoDataModelVersion },
            { "languageDepotRepoName", "LexBox" },
            { "languageDepotRepoUri", repoUrl.ToString() },
            { "user", "LexBox" },
            { "commitMessage", commitMessage }
        };

        var cloneResult = RunClient("lfmergebridge", new ClientRequest("Language_Forge_Send_Receive", flexBridgeOptions, username, password), output);

        if (validateOutput)
        {
            Utils.ValidateSendReceiveOutput(cloneResult);
        }

        return cloneResult;
    }
}

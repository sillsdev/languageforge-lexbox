using Microsoft.Extensions.Configuration;

namespace FwLiteMaui.Tests;

public class FwLiteMauiConfigurationTests : IDisposable
{
    private readonly string _dataDir = Path.Combine(Path.GetTempPath(), "fwlite-config-tests", Guid.NewGuid().ToString("N"));
    private string SettingsFilePath => Path.Combine(_dataDir, FwLiteMauiConfiguration.SettingsFileName);

    public FwLiteMauiConfigurationTests()
    {
        Directory.CreateDirectory(_dataDir);
    }

    public void Dispose()
    {
        Directory.Delete(_dataDir, recursive: true);
    }

    private IConfigurationRoot Build()
    {
        var builder = new ConfigurationBuilder();
        builder.AddFwLiteMauiConfiguration(SettingsFilePath, reloadOnChange: false);
        return builder.Build();
    }

    [Fact]
    public void WithoutSettingsFile_UsesDefaults()
    {
        var config = Build();

        config["Logging:LogLevel:Default"].Should().BeNull();
        config["Logging:LogLevel:FwLiteShared.Auth.LoggerAdapter"].Should().Be("Warning");
        FwLiteMauiConfiguration.IsVerboseLogging(config).Should().BeFalse();
    }

    [Fact]
    public void SettingsFile_OverridesDefaults_ForAllowedKeys()
    {
        File.WriteAllText(SettingsFilePath, """
            {
              // comments are fine
              "Logging": { "LogLevel": { "FwLiteShared.Auth.LoggerAdapter": "Debug" } },
              "FwLiteMaui": { "MaxLogFileCount": 5 }
            }
            """);

        var config = Build();

        config["Logging:LogLevel:FwLiteShared.Auth.LoggerAdapter"].Should().Be("Debug");
        config["Logging:LogLevel:Default"].Should().BeNull();
        config["FwLiteMaui:MaxLogFileCount"].Should().Be("5");
    }

    [Fact]
    public void SettingsFile_CanSetAnySection()
    {
        File.WriteAllText(SettingsFilePath, """{ "FwDataBridge": { "ProjectsFolder": "D:/fw-projects" } }""");

        Build()["FwDataBridge:ProjectsFolder"].Should().Be("D:/fw-projects");
    }

    [Fact]
    public void CorruptSettingsFile_IsSkippedInsteadOfCrashing()
    {
        File.WriteAllText(SettingsFilePath, "{ not json");
        FwLiteMauiConfiguration.LastLoadError = null;

        var config = Build();

        config["Logging:LogLevel:FwLiteShared.Auth.LoggerAdapter"].Should().Be("Warning");
        FwLiteMauiConfiguration.LastLoadError.Should().NotBeNull();
        FwLiteMauiConfiguration.LastLoadError = null;
    }

    [Fact]
    public void EnvironmentVariables_WinOverSettingsFile()
    {
        const string envVar = "FwLiteMaui__MaxLogFileCount";
        File.WriteAllText(SettingsFilePath, """{ "FwLiteMaui": { "MaxLogFileCount": 5 } }""");
        Environment.SetEnvironmentVariable(envVar, "7");
        try
        {
            Build()["FwLiteMaui:MaxLogFileCount"].Should().Be("7");
        }
        finally
        {
            Environment.SetEnvironmentVariable(envVar, null);
        }
    }

    [Fact]
    public void SetVerboseLogging_RoundTrips_AndKeepsOtherSettings()
    {
        File.WriteAllText(SettingsFilePath, """{ "FwLiteMaui": { "MaxLogFileCount": 5 }, "Logging": { "LogLevel": { "LcmCrdt": "Trace" } } }""");
        var config = Build();

        FwLiteMauiConfiguration.SetVerboseLogging(SettingsFilePath, true);
        config.Reload();
        FwLiteMauiConfiguration.IsVerboseLogging(config).Should().BeTrue();
        config["Logging:LogLevel:FwLiteShared.Auth.LoggerAdapter"].Should().Be("Debug");

        FwLiteMauiConfiguration.SetVerboseLogging(SettingsFilePath, false);
        config.Reload();
        FwLiteMauiConfiguration.IsVerboseLogging(config).Should().BeFalse();
        config["Logging:LogLevel:FwLiteShared.Auth.LoggerAdapter"].Should().Be("Warning");
        config["Logging:LogLevel:LcmCrdt"].Should().Be("Trace");
        config["FwLiteMaui:MaxLogFileCount"].Should().Be("5");
    }

    [Fact]
    public void WriteSettingsFile_RejectsInvalidJson_AndKeepsTheOldFile()
    {
        FwLiteMauiConfiguration.WriteSettingsFile(SettingsFilePath, """{ "Logging": { "LogLevel": { "Default": "Debug" } } }""");

        var act = () => FwLiteMauiConfiguration.WriteSettingsFile(SettingsFilePath, "{ not json");
        act.Should().Throw<ArgumentException>().WithMessage("*not valid JSON*");
        var array = () => FwLiteMauiConfiguration.WriteSettingsFile(SettingsFilePath, "[1]");
        array.Should().Throw<ArgumentException>().WithMessage("*JSON object*");

        FwLiteMauiConfiguration.IsVerboseLogging(Build()).Should().BeTrue();
    }

    [Fact]
    public void WriteSettingsFile_BlankDeletesTheFile()
    {
        FwLiteMauiConfiguration.WriteSettingsFile(SettingsFilePath, """{ "Logging": { "LogLevel": { "Default": "Debug" } } }""");
        FwLiteMauiConfiguration.WriteSettingsFile(SettingsFilePath, "  ");

        File.Exists(SettingsFilePath).Should().BeFalse();
        FwLiteMauiConfiguration.IsVerboseLogging(Build()).Should().BeFalse();
    }

    [Fact]
    public void SetVerboseLogging_CreatesTheFileWhenMissing()
    {
        FwLiteMauiConfiguration.SetVerboseLogging(SettingsFilePath, true);

        File.Exists(SettingsFilePath).Should().BeTrue();
        FwLiteMauiConfiguration.IsVerboseLogging(Build()).Should().BeTrue();
    }
}

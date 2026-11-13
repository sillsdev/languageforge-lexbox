using Testing.Services;

namespace Testing.Fixtures;

/// <summary>
/// Fails every test in the class when the hg client isn't the version CI asked for, so a lane can't pass on the wrong hg.
/// </summary>
public class HgClientFixture : IAsyncLifetime
{
    public Task InitializeAsync()
    {
        var expectedMajor = TestingEnvironmentVariables.ExpectedHgMajorVersion;
        if (string.IsNullOrEmpty(expectedMajor))
            throw new InvalidOperationException(
                "Set TEST_EXPECTED_HG_VERSION (and MercurialVersion for the build) to the hg major version under test, e.g. 7");

        var version = SendReceiveService.GetHgVersion();
        if (!version.Contains($"(version {expectedMajor}."))
            throw new InvalidOperationException($"Expected hg {expectedMajor}, but the hg client reports: {version}");
        return Task.CompletedTask;
    }

    public Task DisposeAsync() => Task.CompletedTask;
}

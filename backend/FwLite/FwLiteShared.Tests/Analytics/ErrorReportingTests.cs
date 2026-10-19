using System.Text;
using FwLiteShared.Analytics;
using FwLiteShared.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace FwLiteShared.Tests.Analytics;

public class ErrorReportingTests
{
    [Theory]
    [InlineData("Entry 'banana' not found", "Entry <redacted> not found")]
    [InlineData("Can't open project 'secret'", "Can't open project <redacted>")]
    [InlineData("Entry 'Bob's word' is locked", "Entry <redacted> is locked")]
    [InlineData("Could not find \"my project\"", "Could not find <redacted>")]
    [InlineData("Lookup failed for “ŋgwa”", "Lookup failed for <redacted>")]
    [InlineData(@"Could not open C:\Users\bob\Documents\sena-3.sqlite: locked", "Could not open <path> locked")]
    [InlineData(@"Could not open C:\Users\Jo Smith\My Docs\Big Dict.sqlite: locked", "Could not open <path> locked")]
    [InlineData("No such file /home/jo smith/my dict.sqlite here", "No such file <path> here")]
    [InlineData(@"Missing Documents\x.sqlite now", "Missing <path> now")]
    [InlineData(@"Access denied \\fileserver\share\x.fwdata", "Access denied <path>")]
    [InlineData("No such file /home/bob/.local/share/fwlite/p.sqlite", "No such file <path>")]
    [InlineData("Request to https://lexbox.org/api/crdt/abc?token=1 failed", "Request to <url> failed")]
    [InlineData("User bob@example.com has no access", "User <email> has no access")]
    [InlineData("Sequence contains\r\n  no elements", "Sequence contains no elements")]
    [InlineData("Read and/or write failed", "Read and/or write failed")]
    public void ScrubMessage_RedactsLikelyUserData(string message, string expected)
    {
        ErrorScrubber.ScrubMessage(message).Should().Be(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ScrubMessage_EmptyIsNull(string? message)
    {
        ErrorScrubber.ScrubMessage(message).Should().BeNull();
    }

    [Fact]
    public void ScrubMessage_TruncatesTo255BytesWithoutSplittingCharacters()
    {
        var scrubbed = ErrorScrubber.ScrubMessage(new string('ŋ', 400))!;

        Encoding.UTF8.GetByteCount(scrubbed).Should().BeLessThanOrEqualTo(ErrorScrubber.MaxStringBytes);
        scrubbed.Should().EndWith("…");
        scrubbed.TrimEnd('…').Should().MatchRegex("^ŋ+$");
    }

    [Fact]
    public void ScrubMessage_TruncatesWhenPlaceholdersOverflow()
    {
        // each 3-char quote grows to a 10-char placeholder, so the output overflows before the input ends
        var scrubbed = ErrorScrubber.ScrubMessage(string.Concat(Enumerable.Repeat("'x' ", 50)))!;

        Encoding.UTF8.GetByteCount(scrubbed).Should().BeLessThanOrEqualTo(ErrorScrubber.MaxStringBytes);
        scrubbed.Should().StartWith("<redacted> <redacted>").And.EndWith("…").And.NotContain("'x'");
    }

    [Fact]
    public void ScrubStack_StripsDotnetSourceDirectories()
    {
        var stack = string.Join('\n',
            @"   at FwLiteShared.Services.Foo.Bar(String name) in C:\Users\dev\src\FwLiteShared\Services\Foo.cs:line 42",
            "--- End of stack trace from previous location ---",
            "   at FwLiteShared.Services.Foo.Baz() in /home/runner/work/lexbox/Foo.cs:line 7",
            "   at System.Threading.Tasks.Task.Run()");

        ErrorScrubber.ScrubStack(stack).Should().Equal(
            "at FwLiteShared.Services.Foo.Bar(String name) in Foo.cs:line 42",
            "at FwLiteShared.Services.Foo.Baz() in Foo.cs:line 7",
            "at System.Threading.Tasks.Task.Run()");
    }

    [Fact]
    public void ScrubStack_StripsJsOriginsQueriesAndUserDirectories()
    {
        var stack = string.Join('\n',
            "    at onClick (http://localhost:5173/src/lib/Foo.svelte?t=1712:10:5)",
            "save@https://0.0.0.0/_content/viewer/index.js:1:2345",
            "    at file:///C:/Users/bob/AppData/viewer.js:3:4");

        ErrorScrubber.ScrubStack(stack).Should().Equal(
            "at onClick (/src/lib/Foo.svelte:10:5)",
            "save@/_content/viewer/index.js:1:2345",
            "at /C:/Users/<user>/AppData/viewer.js:3:4");
    }

    [Fact]
    public void ScrubStack_CapsTotalSize()
    {
        var stack = string.Join('\n', Enumerable.Range(0, 500).Select(i => $"   at Namespace.Type.Method{i}()"));

        var frames = ErrorScrubber.ScrubStack(stack);

        frames.Should().NotBeEmpty();
        frames.Sum(f => Encoding.UTF8.GetByteCount(f) + 3).Should().BeLessThanOrEqualTo(ErrorScrubber.MaxListBytes);
        frames[0].Should().Be("at Namespace.Type.Method0()");
    }

    [Fact]
    public void BuildProperties_DotnetException()
    {
        var exception = Thrown(new InvalidOperationException("Project 'secret' failed",
            new IOException(@"Could not read C:\Users\bob\x.sqlite")));

        var props = ErrorReporting.BuildProperties("FwLiteShared.Foo", LogLevel.Error, "Failed to open {Project}", exception);

        props["error_source"].Should().Be(ErrorReporting.DotnetSource);
        props["log_category"].Should().Be("FwLiteShared.Foo");
        props["log_level"].Should().Be("Error");
        props["message_template"].Should().Be("Failed to open {Project}");
        props["exception_type"].Should().Be("System.InvalidOperationException");
        props["exception_message"].Should().Be("Project <redacted> failed");
        props["exception_stack"].Should().BeOfType<List<string>>().Which.Should().NotBeEmpty();
        props["inner_exceptions"].Should().BeEquivalentTo(new[] { "System.IO.IOException: Could not read <path>" });
    }

    [Fact]
    public void BuildProperties_NoException()
    {
        var props = ErrorReporting.BuildProperties("Cat", LogLevel.Critical, "Something broke", null);

        props["log_level"].Should().Be("Critical");
        props.Should().NotContainKey("exception_type");
        props.Should().NotContainKey("exception_stack");
    }

    [Theory]
    [InlineData("TypeError: x is undefined", "TypeError")]
    [InlineData("System.InvalidOperationException: broken", "System.InvalidOperationException")]
    [InlineData("Script error.", "Error")]
    public void BuildProperties_ViewerError(string message, string expectedType)
    {
        var exception = new JsErrorException(message, "    at foo (http://localhost:5173/src/a.ts:1:2)");

        var props = ErrorReporting.BuildProperties("FwLiteShared.Services.JsInvokableLogger", LogLevel.Error, "Viewer error", exception);

        props["error_source"].Should().Be(ErrorReporting.JsSource);
        props["exception_type"].Should().Be(expectedType);
        props["exception_stack"].Should().BeEquivalentTo(new[] { "at foo (/src/a.ts:1:2)" });
    }

    [Fact]
    public void LoggerProvider_ReportsErrorsWithTemplateNotFormattedMessage()
    {
        var (analytics, logger) = CreateLogger("FwLiteShared.Projects.Foo");
        IReadOnlyDictionary<string, object?>? sent = null;
        analytics.Setup(a => a.Track(ErrorReporting.ErrorEvent, It.IsAny<IReadOnlyDictionary<string, object?>>(), null))
            .Callback<string, IReadOnlyDictionary<string, object?>?, DateTimeOffset?>((_, p, _) => sent = p);

        logger.LogError(new InvalidOperationException("boom"), "Failed to open {Project}", "my-secret-project");

        sent.Should().NotBeNull();
        sent!["message_template"].Should().Be("Failed to open {Project}");
        sent["exception_type"].Should().Be("System.InvalidOperationException");
        sent.Values.OfType<string>().Should().NotContain(v => v.Contains("my-secret-project"));
    }

    [Fact]
    public void LoggerProvider_IgnoresWarningsAndAnalyticsCategories()
    {
        var (analytics, logger) = CreateLogger("FwLiteShared.Foo");
        var (analytics2, analyticsLogger) = CreateLogger(typeof(AnalyticsService).FullName!);

        logger.LogWarning("just a warning");
        analyticsLogger.LogError("Failed to send analytics event");

        analytics.Verify(a => a.Track(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<DateTimeOffset?>()), Times.Never);
        analytics2.Verify(a => a.Track(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public void LoggerProvider_RespectsOptOut()
    {
        var (analytics, logger) = CreateLogger("FwLiteShared.Foo", enabled: false);

        logger.LogError("broken");

        analytics.Verify(a => a.Track(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<DateTimeOffset?>()), Times.Never);
    }

    [Fact]
    public void LoggerProvider_CapsReportsPerMinute()
    {
        var (analytics, logger) = CreateLogger("FwLiteShared.Foo");

        for (var i = 0; i < AnalyticsErrorLoggerProvider.MaxReportsPerMinute + 10; i++)
            logger.LogError("broken");

        analytics.Verify(a => a.Track(ErrorReporting.ErrorEvent, It.IsAny<IReadOnlyDictionary<string, object?>>(), null),
            Times.Exactly(AnalyticsErrorLoggerProvider.MaxReportsPerMinute));
    }

    [Fact]
    public void LoggerProvider_NeverThrows()
    {
        var (analytics, logger) = CreateLogger("FwLiteShared.Foo");
        analytics.Setup(a => a.Track(It.IsAny<string>(), It.IsAny<IReadOnlyDictionary<string, object?>>(), It.IsAny<DateTimeOffset?>()))
            .Throws<InvalidOperationException>();

        var act = () => logger.LogError("broken");

        act.Should().NotThrow();
    }

    private static (Mock<IAnalyticsService> Analytics, ILogger Logger) CreateLogger(string category, bool enabled = true)
    {
        var analytics = new Mock<IAnalyticsService>();
        analytics.Setup(a => a.GetAnalyticsEnabled()).Returns(enabled);
        var services = new ServiceCollection().AddSingleton(analytics.Object).BuildServiceProvider();
        var provider = new AnalyticsErrorLoggerProvider(services);
        return (analytics, provider.CreateLogger(category));
    }

    private static Exception Thrown(Exception exception)
    {
        try
        {
            throw exception;
        }
        catch (Exception e)
        {
            return e;
        }
    }
}

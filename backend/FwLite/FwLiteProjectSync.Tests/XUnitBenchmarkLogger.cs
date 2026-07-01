using System.Text;
using BenchmarkDotNet.Loggers;
using Xunit.Abstractions;

namespace FwLiteProjectSync.Tests;

/// <summary>
/// Bridges BenchmarkDotNet's line-buffered logger to xUnit's <see cref="ITestOutputHelper"/>.
/// BDN emits one line via several <c>Write</c> calls followed by <c>WriteLine</c>; we buffer and flush.
/// </summary>
internal sealed class XUnitBenchmarkLogger(ITestOutputHelper output) : ILogger
{
    public string Id => nameof(XUnitBenchmarkLogger);
    public int Priority => 0;
    private readonly StringBuilder _line = new();

    public void Write(LogKind logKind, string text)
    {
        _line.Append(text);
    }

    public void WriteLine()
    {
        output.WriteLine(_line.ToString());
        _line.Clear();
    }

    public void WriteLine(LogKind logKind, string text)
    {
        _line.Append(text);
        WriteLine();
    }

    public void Flush()
    {
        if (_line.Length == 0) return;
        WriteLine();
    }
}

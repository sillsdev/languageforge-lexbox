using SIL.Progress;
using Xunit.Abstractions;

namespace Testing.Logging
{
    public class XunitStringBuilderProgress : StringBuilderProgress
    {
        private readonly ITestOutputHelper _output;

        public XunitStringBuilderProgress(ITestOutputHelper output)
        {
            _output = output;
        }

        public override void WriteMessage(string message, params object[] args)
        {
            // Chorus logs things like 'log -r0 --template "{node}"' which causes C# to throw string formatting
            // exceptions, so we only use the formatting version (with args) if format args were actually passed.
            if (args is { Length: > 0 })
            {
                _output.WriteLine(message, args);
            }
            else
            {
                _output.WriteLine(message);
            }
            base.WriteMessage(message, args);
        }

        // No need to override WriteMessageWithColor here, as the StringBuilderProgress class has WriteMessageWithColor call WriteMessage
    }
}


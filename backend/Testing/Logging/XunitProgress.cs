// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using SIL.Progress;
using Xunit.Abstractions;

namespace Testing.Logging
{
    public class XunitProgress : GenericProgress
    {
        private ITestOutputHelper _output { get; init; }

        public XunitProgress(ITestOutputHelper output)
        {
            _output = output;
        }

        public override void WriteMessage(string message, params object[] args)
        {
            _output.WriteLine(message, args);
        }

        public override void WriteMessageWithColor(string colorName, string message, params object[] args)
        {
            // Ignore color in xunit messages
            WriteMessage(message, args);
        }
    }
}


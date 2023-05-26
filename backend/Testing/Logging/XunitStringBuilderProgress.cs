// Copyright (c) 2016 SIL International
// This software is licensed under the MIT license (http://opensource.org/licenses/MIT)
using SIL.Progress;
using Xunit.Abstractions;

namespace Testing.Logging
{
    public class XunitStringBuilderProgress : StringBuilderProgress
    {
        private ITestOutputHelper _output { get; init; }
        // public string Text { get; } // Inherited from base class

        public XunitStringBuilderProgress(ITestOutputHelper output)
        {
            _output = output;
        }

        public override void WriteMessage(string message, params object[] args)
        {
            _output.WriteLine(message, args);
            base.WriteMessage(message, args);
        }

        // No need to override WriteMessageWithColor here, as the StringBuilderProgress class has WriteMessageWithColor call WriteMessage
    }
}


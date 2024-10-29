﻿// Copyright (c) 2011-2024 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.ComponentModel;
using Microsoft.Extensions.Logging;
using SIL.LCModel.FixData;
using SIL.LCModel.Utils;

namespace FixFwData;

internal class Program
{
    private static int Main(string[] args)
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        logger = loggerFactory.CreateLogger("FixFwData");
        var pathname = args[0];
        var prog = new LoggingProgress(logger);
        var data = new FwDataFixer(pathname, prog, logError, getErrorCount);
        data.FixErrorsAndSave();
        return errorsOccurred ? 1 : 0;
    }

    private static bool errorsOccurred = false;
    private static int errorCount = 0;
    private static ILogger? logger;

    private static void logError(string description, bool errorFixed)
    {
        logger?.LogError(description);

        errorsOccurred = true;
        if (errorFixed)
            ++errorCount;
    }

    private static int getErrorCount()
    {
        return errorCount;
    }

    private sealed class LoggingProgress(ILogger logger) : IProgress
    {
        public string Message { get => ""; set => logger.LogInformation(value); }

        #region Do-nothing implementation of IProgress GUI methods
        // IProgress methods required by the interface that don't make sense in a console app
        public event CancelEventHandler? Canceling;
        public void Step(int amount)
        {
            if (Canceling != null)
            {
                // don't do anything -- this just shuts up the compiler about the
                // event handler never being used.
            }
        }

        public string Title { get => ""; set { } }
        public int Position { get; set; }
        public int StepSize { get; set; }
        public int Minimum { get; set; }
        public int Maximum { get; set; }
        public ISynchronizeInvoke? SynchronizeInvoke { get => null; private set { } }
        public bool IsIndeterminate { get => false; set { } }
        public bool AllowCancel { get => false; set { } }
        #endregion
    }
}

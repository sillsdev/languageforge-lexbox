using System.Diagnostics;

namespace FwLiteProjectSync;

public class FwLiteProjectSyncActivitySource
{
    public const string ActivitySourceName = "FwLiteProjectSync";
    internal static readonly ActivitySource Value = new(ActivitySourceName);
}

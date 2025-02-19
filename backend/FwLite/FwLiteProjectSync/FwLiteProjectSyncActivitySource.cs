using System.Diagnostics;

namespace FwLiteProjectSync;

public class FwLiteProjectSyncActivitySource
{
    public const string ActivitySourceName = "FwLiteProjectSync";
    public static readonly ActivitySource Value = new ActivitySource(ActivitySourceName);
}

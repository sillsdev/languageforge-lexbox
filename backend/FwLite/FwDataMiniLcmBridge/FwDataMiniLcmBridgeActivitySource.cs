using System.Diagnostics;

namespace FwDataMiniLcmBridge;

public static class FwDataMiniLcmBridgeActivitySource
{
    public const string ActivitySourceName = "FwDataMiniLcmBridge";
    public static readonly ActivitySource Value = new(ActivitySourceName);
}

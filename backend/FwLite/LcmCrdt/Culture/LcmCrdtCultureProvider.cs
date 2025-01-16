using System.Globalization;
using Microsoft.Extensions.Logging;
using MiniLcm.Culture;

namespace LcmCrdt.Culture;

public class LcmCrdtCultureProvider(ILogger<LcmCrdtCultureProvider> logger) : IMiniLcmCultureProvider
{
    public CompareInfo GetCompareInfo(WritingSystem? writingSystem)
    {
        if (writingSystem == null || writingSystem.WsId == WritingSystemId.Default)
        {
            return CultureInfo.InvariantCulture.CompareInfo;
        }

        try
        {
            //todo use ICU/SLDR instead
            return CultureInfo.CreateSpecificCulture(writingSystem.WsId.Code).CompareInfo;
        }
        catch (Exception e)
        {
            logger.LogWarning(e, "Failed to create compare info for '{WritingSystemId}'", writingSystem.WsId);
            return CultureInfo.InvariantCulture.CompareInfo;
        }
    }
}

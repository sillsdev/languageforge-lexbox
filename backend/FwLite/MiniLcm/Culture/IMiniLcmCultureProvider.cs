using System.Globalization;
using MiniLcm.Models;

namespace MiniLcm.Culture;

public interface IMiniLcmCultureProvider
{
    CompareInfo GetCompareInfo(WritingSystem? writingSystem);
}

using Microsoft.Extensions.Caching.Memory;
using MiniLcm;

namespace LfClassicData;

public class LfClassicLexboxApiProvider(ProjectDbContext dbContext, SystemDbContext systemDbContext, IMemoryCache memoryCache)
{
    public IMiniLcmReadApi GetProjectApi(string projectCode)
    {
        return new LfClassicMiniLcmApi(projectCode, dbContext, systemDbContext, memoryCache);
    }
}

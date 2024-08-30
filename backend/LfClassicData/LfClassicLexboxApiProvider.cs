using MiniLcm;

namespace LfClassicData;

public class LfClassicLexboxApiProvider(ProjectDbContext dbContext, SystemDbContext systemDbContext)
{
    public IMiniLcmReadApi GetProjectApi(string projectCode)
    {
        return new LfClassicMiniLcmApi(projectCode, dbContext, systemDbContext);
    }
}

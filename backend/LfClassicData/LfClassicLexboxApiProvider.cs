using MiniLcm;

namespace LfClassicData;

public class LfClassicLexboxApiProvider(ProjectDbContext dbContext, SystemDbContext systemDbContext) : ILexboxApiProvider
{
    public IMiniLcmApi GetProjectApi(string projectCode)
    {
        return new LfClassicMiniLcmApi(projectCode, dbContext, systemDbContext);
    }
}

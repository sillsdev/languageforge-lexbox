using MiniLcm;

namespace LfClassicData;

public class LfClassicLexboxApiProvider(ProjectDbContext dbContext, SystemDbContext systemDbContext) : ILexboxApiProvider
{
    public ILexboxApi GetProjectApi(string projectCode)
    {
        return new LfClassicLexboxApi(projectCode, dbContext, systemDbContext);
    }
}

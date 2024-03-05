using MiniLcm;

namespace LfClassicData;

public class LfClassicLexboxApiProvider(ProjectDbContext dbContext) : ILexboxApiProvider
{
    public ILexboxApi GetProjectApi(string projectCode)
    {
        return new LfClassicLexboxApi(projectCode, dbContext);
    }
}

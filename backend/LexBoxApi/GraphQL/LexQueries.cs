using LexCore.Entities;
using LexData;

namespace LexBoxApi.GraphQL;

public class LexQueries
{
    [UseFirstOrDefault]
    [UseProjection]
    public IQueryable<Project> Project(LexBoxDbContext context)
    {
        return context.Projects;
    }
}
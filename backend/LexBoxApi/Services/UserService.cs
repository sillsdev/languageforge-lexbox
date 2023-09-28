using LexData;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class UserService
{
    private readonly LexBoxDbContext _dbContext;

    public UserService(LexBoxDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task UpdateUserLastActive(Guid id)
    {
        await _dbContext.Users.Where(u => u.Id == id)
            .ExecuteUpdateAsync(c => c.SetProperty(u => u.LastActive, DateTimeOffset.UtcNow));
    }
}

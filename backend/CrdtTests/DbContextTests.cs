using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Tests;

public class DbContextTests: DataModelTestBase
{
    [Fact]
    public async Task VerifyModel()
    {
        await Verify(DbContext.Model.ToDebugString(MetadataDebugStringOptions.LongDefault));
    }
}
using Microsoft.EntityFrameworkCore;

namespace LexData;

public class ConfigureDbModel(Action<ModelBuilder> action)
{
    public void Configure(ModelBuilder modelBuilder)
    {
        action(modelBuilder);
    }
}

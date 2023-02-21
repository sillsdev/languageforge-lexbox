using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexData.EntityIds;

public static class EfLfIdExtensions
{
    public static void UseLfIdConverters(this DbContextOptionsBuilder builder)
    {
        builder.ReplaceService<IValueConverterSelector, LfIdValueConverterSelector>();
    }
}
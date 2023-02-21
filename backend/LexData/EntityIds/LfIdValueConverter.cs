using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LexData.EntityIds;

public class LfIdValueConverter<TEntity> : ValueConverter<LfId<TEntity>, Guid>
{
    public LfIdValueConverter(
        ) : base(id => id.GetIdForDb(),
        guid => LfId.FromDb<TEntity>(guid))
    {
    }
}
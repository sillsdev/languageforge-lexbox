using LexCore.Entities;
using LinqToDB.Mapping;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Configuration;

public class EntityBaseConfiguration<T>: IEntityTypeConfiguration<T> where T :  EntityBase
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.CreatedDate).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedDate).HasDefaultValueSql("now()");
    }
}

public interface ILinq2DbEntityConfiguration<T>
{
    static abstract void ConfigureLinq2Db(EntityMappingBuilder<T> entity);
}

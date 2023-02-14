using LexData.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Configuration;

public class EntityBaseConfiguration<T>: IEntityTypeConfiguration<T> where T :  EntityBase
{
    public virtual void Configure(EntityTypeBuilder<T> builder)
    {
        builder.Property(e => e.CreatedDate).HasDefaultValueSql("now()");
        builder.Property(e => e.UpdatedDate).HasDefaultValueSql("now()");
    }
}
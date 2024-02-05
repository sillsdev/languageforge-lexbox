using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class FlexProjectMetadataEntityConfiguration: IEntityTypeConfiguration<FlexProjectMetadata>
{
    public virtual void Configure(EntityTypeBuilder<FlexProjectMetadata> builder)
    {
        builder.HasKey(e => e.ProjectId);
    }
}

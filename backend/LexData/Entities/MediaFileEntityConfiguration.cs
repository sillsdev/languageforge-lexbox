using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class MediaFileEntityConfiguration : IEntityTypeConfiguration<MediaFile>
{
    public virtual void Configure(EntityTypeBuilder<MediaFile> builder)
    {
        builder.HasKey(e => e.Id);
        builder.HasOne<Project>().WithMany()
            .HasPrincipalKey(project => project.Id)
            .HasForeignKey(c => c.ProjectId);
        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        builder.OwnsOne(u => u.Metadata, mb =>
        {
            mb.ToJson();
        });
        // TODO: Check if we also need to add:
        // builder.Property(u => u.Metadata)
        //     .IsRequired(false)
        //     .HasDefaultValueSql("'{}'");
    }
}

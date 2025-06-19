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
        var jsonOptions = new JsonSerializerOptions
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
        builder.Property(u => u.Metadata)
            .IsRequired(false)
            .HasDefaultValueSql("'{}'")
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<FileMetadata>(v, jsonOptions)
            );
    }
}

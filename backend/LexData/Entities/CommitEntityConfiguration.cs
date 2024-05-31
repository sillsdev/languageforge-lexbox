using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crdt.Core;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LexData.Entities;

public class CommitEntityConfiguration : IEntityTypeConfiguration<ServerCommit>
{
    public void Configure(EntityTypeBuilder<ServerCommit> builder)
    {
        builder.ToTable("CrdtCommits");
        builder.HasKey(c => c.Id);
        builder.ComplexProperty(c => c.HybridDateTime);
        builder.HasOne<FlexProjectMetadata>().WithMany()
            .HasPrincipalKey(f => f.ProjectId)
            .HasForeignKey(c => c.ProjectId);
        builder.Property(c => c.Metadata).HasConversion(
            m => JsonSerializer.Serialize(m, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<CommitMetadata>(json, (JsonSerializerOptions?)null) ?? new()
        );
        builder.OwnsMany(c => c.ChangeEntities,
            b => b.ToJson().Property(c => c.Change).HasConversion(
                change => Serialize(change),
                element => Deserialize(element)
            ));
    }

    private static ServerJsonChange Deserialize(string s) => JsonSerializer.Deserialize<ServerJsonChange>(s)!;

    private static string Serialize(ServerJsonChange c) => JsonSerializer.Serialize(c);
}

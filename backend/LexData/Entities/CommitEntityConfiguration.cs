﻿using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using Crdt.Core;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LexData.Entities;

public class CommitEntityConfiguration : IEntityTypeConfiguration<CrdtCommit>
{
    public void Configure(EntityTypeBuilder<CrdtCommit> builder)
    {
        builder.ToTable("CrdtCommits");
        builder.HasKey(c => c.Id);
        builder.ComplexProperty(c => c.HybridDateTime);
        builder.HasOne<FlexProjectMetadata>().WithMany()
            .HasPrincipalKey(f => f.ProjectId)
            .HasForeignKey(c => c.ProjectId);
        builder.OwnsMany(c => c.ChangeEntities,
            b => b.ToJson().Property(c => c.Change).HasConversion(
                change => Serialize(change),
                element => Deserialize(element)
            ));
    }

    private static JsonChange Deserialize(string s) => JsonSerializer.Deserialize<JsonChange>(s)!;

    private static string Serialize(JsonChange c) => JsonSerializer.Serialize(c);
}

public class CrdtCommit : CommitBase<JsonChange>
{
    [JsonConstructor]
    protected CrdtCommit(Guid id, string hash, string parentHash, HybridDateTime hybridDateTime) : base(id,
        hash,
        parentHash,
        hybridDateTime)
    {
    }

    public CrdtCommit(Guid id) : base(id)
    {
    }

    public CrdtCommit()
    {
    }

    public Guid ProjectId { get; set; }
}

public class JsonChange
{
    [JsonPropertyName("$type"), JsonPropertyOrder(1)]
    public required string Type { get; set; }

    [JsonExtensionData, JsonPropertyOrder(2)]
    public Dictionary<string, JsonElement>? ExtensionData { get; set; }

    public static implicit operator JsonChange(JsonElement e) =>
        e.Deserialize<JsonChange>() ??
        throw new SerializationException("Failed to deserialize JSON change");
}

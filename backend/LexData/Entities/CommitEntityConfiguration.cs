using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;
using SIL.Harmony.Core;
using LexCore.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace LexData.Entities;

public class CommitEntityConfiguration : IEntityTypeConfiguration<ServerCommit>
{
    private static JsonSerializerOptions Options = new JsonSerializerOptions()
    {
        Converters = { new ServerJsonChangeConverter() }
    };
    public void Configure(EntityTypeBuilder<ServerCommit> builder)
    {
        builder.ToTable("CrdtCommits");
        builder.HasKey(c => c.Id);
        builder.ComplexProperty(c => c.HybridDateTime);
        builder.HasOne<Project>().WithMany()
            .HasPrincipalKey(project => project.Id)
            .HasForeignKey(c => c.ProjectId);
        builder.Property(c => c.Metadata).HasConversion(
            m => JsonSerializer.Serialize(m, (JsonSerializerOptions?)null),
            json => JsonSerializer.Deserialize<CommitMetadata>(json, (JsonSerializerOptions?)null) ?? new()
        );
        builder.Property(c => c.ChangeEntities).HasConversion(
            c => JsonSerializer.Serialize(c, Options),
            json => JsonSerializer.Deserialize<List<ChangeEntity<ServerJsonChange>>>(json, Options) ?? new()
        ).HasColumnType("jsonb").IsRequired(false);
    }

    private static ServerJsonChange Deserialize(string s) => JsonSerializer.Deserialize<ServerJsonChange>(s)!;

    private static string Serialize(ServerJsonChange c) => JsonSerializer.Serialize(c);

    private class ServerJsonChangeConverter: JsonConverter<ServerJsonChange>
    {
        public override ServerJsonChange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions _)
        {
            //ignoring JsonSerializerOptions, otherwise we would stack overflow recursively using this converter
            if (typeToConvert != typeof(ServerJsonChange))
                throw new SerializationException("type not supported " + typeToConvert.FullName);
            //special case, this object type used to be encoded as a string in json like this: {"Change": "{\"Prop\": 1}"}
            if (reader.TokenType == JsonTokenType.String)
            {
                var json = reader.GetString();
                if (json is null) return null;
                return JsonSerializer.Deserialize<ServerJsonChange>(json);
            }

            return JsonSerializer.Deserialize<ServerJsonChange>(ref reader);
        }

        public override void Write(Utf8JsonWriter writer, ServerJsonChange value, JsonSerializerOptions options)
        {
            //not passing in options otherwise it would just use this converter again
            JsonSerializer.Serialize(writer, value);
        }
    }
}

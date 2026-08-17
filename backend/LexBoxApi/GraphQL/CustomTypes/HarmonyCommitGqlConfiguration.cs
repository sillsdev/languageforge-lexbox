using System.Text.Json;
using SIL.Harmony.Core;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class HarmonyCommitGqlConfiguration : ObjectType<ServerCommit>
{
    protected override void Configure(IObjectTypeDescriptor<ServerCommit> descriptor)
    {
        descriptor.BindFieldsExplicitly();
        descriptor.Field(h => h.Id);
        descriptor.Field(h => h.HybridDateTime);
        descriptor.Field(h => h.ProjectId);
        descriptor.Field(h => h.ClientId);
        descriptor.Field(h => h.Metadata).Type<JsonType>()
            .Resolve(context =>
            {
                var metadata = context.Parent<ServerCommit>().Metadata;
                return JsonSerializer.SerializeToElement(metadata);
            });
    }
}

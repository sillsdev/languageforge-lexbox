using HotChocolate.Stitching;

namespace LexBoxApi.GraphQL;

public class ProjectExtensions : ObjectTypeExtension
{
    protected override void Configure(IObjectTypeDescriptor descriptor)
    {
        // adds a new field to the Project type
        // this field delegates the resolution to the LexQueries.Changesets method.
        descriptor.Name("Projects")
            .Field("changesets")
            .Type("[Changeset!]!")
            .Directive(new DelegateDirective($"{nameof(LexQueries.Changesets).ToLower()}(projectCode: $fields:code)", GraphQlSetupKernel.LexBoxSchemaName));
    }
}
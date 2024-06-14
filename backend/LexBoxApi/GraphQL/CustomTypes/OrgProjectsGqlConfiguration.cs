﻿using LexCore.Entities;

namespace LexBoxApi.GraphQL.CustomTypes;

[ObjectType]
public class OrgProjectsGqlConfiguration : ObjectType<OrgProjects>
{
    protected override void Configure(IObjectTypeDescriptor<OrgProjects> descriptor)
    {
        descriptor.Field(f => f.Org).Type<NonNullType<OrgGqlConfiguration>>();
        descriptor.Field(f => f.Project).Type<NonNullType<ProjectGqlConfiguration>>();
    }
}

﻿using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class OrganizationEntityConfiguration: EntityBaseConfiguration<Organization>
{
    public override void Configure(EntityTypeBuilder<Organization> builder)
    {
        base.Configure(builder);
        builder.ToTable("Orgs");
        builder.HasIndex(o => o.Name).IsUnique();
        builder.HasMany(o => o.Members)
            .WithOne(m => m.Organization)
            .HasForeignKey(m => m.OrgId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(o => o.Projects)
            .WithMany(p => p.Organizations)
            .UsingEntity<OrgProjects>(
                op => op.HasOne(op => op.Project).WithMany().HasForeignKey(op => op.ProjectId),
                op => op.HasOne(op => op.Org).WithMany().HasForeignKey(op => op.OrgId)
            );
    }
}

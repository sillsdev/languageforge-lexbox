using System;
using System.Collections.Generic;
using LexData.Migrations;
using Microsoft.EntityFrameworkCore;

namespace LexData.Redmine;

public partial class RedmineDbContext : DbContext
{
    public RedmineDbContext()
    {
    }

    protected RedmineDbContext(DbContextOptions options) : base(options)
    {
    }

    public virtual DbSet<RmEmailAddress> EmailAddresses { get; set; }

    public virtual DbSet<RmMember> Members { get; set; }

    public virtual DbSet<RmMemberRole> MemberRoles { get; set; }

    public virtual DbSet<RmProject> Projects { get; set; }

    public virtual DbSet<RmRole> Roles { get; set; }

    public virtual DbSet<RmUser> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RmEmailAddress>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("email_addresses");

            entity.HasIndex(e => e.UserId, "index_email_addresses_on_user_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Address)
                .HasMaxLength(255)
                .HasColumnName("address");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.IsDefault).HasColumnName("is_default");
            entity.Property(e => e.Notify)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("notify");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<RmMember>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("members");

            entity.HasIndex(e => e.ProjectId, "index_members_on_project_id");

            entity.HasIndex(e => e.UserId, "index_members_on_user_id");

            entity.HasIndex(e => new { e.UserId, e.ProjectId }, "index_members_on_user_id_and_project_id").IsUnique();

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.MailNotification).HasColumnName("mail_notification");
            entity.Property(e => e.ProjectId)
                .HasColumnType("int(11)")
                .HasColumnName("project_id");
            entity.Property(e => e.UserId)
                .HasColumnType("int(11)")
                .HasColumnName("user_id");
        });

        modelBuilder.Entity<RmMemberRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("member_roles");

            entity.HasIndex(e => e.InheritedFrom, "index_member_roles_on_inherited_from");

            entity.HasIndex(e => e.MemberId, "index_member_roles_on_member_id");

            entity.HasIndex(e => e.RoleId, "index_member_roles_on_role_id");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.InheritedFrom)
                .HasColumnType("int(11)")
                .HasColumnName("inherited_from");
            entity.Property(e => e.MemberId)
                .HasColumnType("int(11)")
                .HasColumnName("member_id");
            entity.Property(e => e.RoleId)
                .HasColumnType("int(11)")
                .HasColumnName("role_id");
        });

        modelBuilder.Entity<RmProject>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("projects");

            entity.HasIndex(e => e.Lft, "index_projects_on_lft");

            entity.HasIndex(e => e.Rgt, "index_projects_on_rgt");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.DefaultAssignedToId)
                .HasColumnType("int(11)")
                .HasColumnName("default_assigned_to_id");
            entity.Property(e => e.DefaultVersionId)
                .HasColumnType("int(11)")
                .HasColumnName("default_version_id");
            entity.Property(e => e.Description)
                .HasColumnType("text")
                .HasColumnName("description");
            entity.Property(e => e.Homepage)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("homepage");
            entity.Property(e => e.Identifier)
                .HasMaxLength(255)
                .HasColumnName("identifier");
            entity.Property(e => e.InheritMembers).HasColumnName("inherit_members");
            entity.Property(e => e.IsPublic)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("is_public");
            entity.Property(e => e.Lft)
                .HasColumnType("int(11)")
                .HasColumnName("lft");
            entity.Property(e => e.Name)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.ParentId)
                .HasColumnType("int(11)")
                .HasColumnName("parent_id");
            entity.Property(e => e.Rgt)
                .HasColumnType("int(11)")
                .HasColumnName("rgt");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("status");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
        });

        modelBuilder.Entity<RmRole>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("roles");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.AllRolesManaged)
                .IsRequired()
                .HasDefaultValueSql("'1'")
                .HasColumnName("all_roles_managed");
            entity.Property(e => e.Assignable)
                .HasDefaultValueSql("'1'")
                .HasColumnName("assignable");
            entity.Property(e => e.Builtin)
                .HasColumnType("int(11)")
                .HasColumnName("builtin");
            entity.Property(e => e.IssuesVisibility)
                .HasMaxLength(30)
                .HasDefaultValueSql("'default'")
                .HasColumnName("issues_visibility");
            entity.Property(e => e.Name)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("name");
            entity.Property(e => e.Permissions)
                .HasColumnType("text")
                .HasColumnName("permissions");
            entity.Property(e => e.Position)
                .HasColumnType("int(11)")
                .HasColumnName("position");
            entity.Property(e => e.Settings)
                .HasColumnType("text")
                .HasColumnName("settings");
            entity.Property(e => e.TimeEntriesVisibility)
                .HasMaxLength(30)
                .HasDefaultValueSql("'all'")
                .HasColumnName("time_entries_visibility");
            entity.Property(e => e.UsersVisibility)
                .HasMaxLength(30)
                .HasDefaultValueSql("'all'")
                .HasColumnName("users_visibility");
        });

        modelBuilder.Entity<RmUser>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("PRIMARY");

            entity.ToTable("users");

            entity.HasIndex(e => e.AuthSourceId, "index_users_on_auth_source_id");

            entity.HasIndex(e => new { e.Id, e.Type }, "index_users_on_id_and_type");

            entity.HasIndex(e => e.Type, "index_users_on_type");

            entity.Property(e => e.Id)
                .HasColumnType("int(11)")
                .HasColumnName("id");
            entity.Property(e => e.Admin).HasColumnName("admin");
            entity.Property(e => e.AuthSourceId)
                .HasColumnType("int(11)")
                .HasColumnName("auth_source_id");
            entity.Property(e => e.CreatedOn)
                .HasColumnType("datetime")
                .HasColumnName("created_on");
            entity.Property(e => e.Firstname)
                .HasMaxLength(30)
                .HasDefaultValueSql("''")
                .HasColumnName("firstname");
            entity.Property(e => e.HashedPassword)
                .HasMaxLength(40)
                .HasDefaultValueSql("''")
                .HasColumnName("hashed_password");
            entity.Property(e => e.IdentityUrl)
                .HasMaxLength(255)
                .HasColumnName("identity_url");
            entity.Property(e => e.Language)
                .HasMaxLength(5)
                .HasDefaultValueSql("''")
                .HasColumnName("language");
            entity.Property(e => e.LastLoginOn)
                .HasColumnType("datetime")
                .HasColumnName("last_login_on");
            entity.Property(e => e.Lastname)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("lastname");
            entity.Property(e => e.Login)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("login");
            entity.Property(e => e.MailNotification)
                .HasMaxLength(255)
                .HasDefaultValueSql("''")
                .HasColumnName("mail_notification");
            entity.Property(e => e.MustChangePasswd).HasColumnName("must_change_passwd");
            entity.Property(e => e.PasswdChangedOn)
                .HasColumnType("datetime")
                .HasColumnName("passwd_changed_on");
            entity.Property(e => e.Salt)
                .HasMaxLength(64)
                .HasColumnName("salt");
            entity.Property(e => e.Status)
                .HasDefaultValueSql("'1'")
                .HasColumnType("int(11)")
                .HasColumnName("status");
            entity.Property(e => e.Type).HasColumnName("type");
            entity.Property(e => e.UpdatedOn)
                .HasColumnType("datetime")
                .HasColumnName("updated_on");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}

public class PrivateRedmineDbContext: RedmineDbContext
{
    public PrivateRedmineDbContext(DbContextOptions<PrivateRedmineDbContext> options) : base(options)
    {
    }
}

public class PublicRedmineDbContext: RedmineDbContext
{
    public PublicRedmineDbContext(DbContextOptions<PublicRedmineDbContext> options) : base(options)
    {
    }
}

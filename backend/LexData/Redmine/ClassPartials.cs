using Microsoft.EntityFrameworkCore;

namespace LexData.Redmine;

public partial class RedmineDbContext
{
    partial void OnModelCreatingPartial(ModelBuilder modelBuilder)
    {
        var userEntity = modelBuilder.Entity<RmUser>();
        userEntity.HasMany(u => u.EmailAddresses).WithOne(e => e.User);
        userEntity.HasMany(u => u.ProjectMembership).WithOne(m => m.User);

        var memberEntity = modelBuilder.Entity<RmMember>();
        memberEntity.HasOne(m => m.Role).WithOne(r => r.Member);
        

        var projectEntity = modelBuilder.Entity<RmProject>();
        projectEntity.HasMany(p => p.Members).WithOne(m => m.Project);
        projectEntity.HasOne(p => p.Parent).WithMany(p => p.Children);
        
        var memberRoleEntity = modelBuilder.Entity<RmMemberRole>();
        memberRoleEntity.HasOne(mr => mr.Role).WithMany(r => r.MemberRoles);
    }
}

public partial class RmUser
{
    public required List<RmEmailAddress> EmailAddresses { get; set; }
    public required List<RmMember> ProjectMembership { get; set; }
}

public partial class RmEmailAddress
{
    public required RmUser User { get; set; }
}

public partial class RmMember
{
    public required RmUser User { get; set; }
    public required RmMemberRole Role { get; set; }
    public required RmProject Project { get; set; }
}

public partial class RmMemberRole
{
    public required RmMember Member { get; set; }
    public required RmRole Role { get; set; }
}

public partial class RmProject
{
    public required List<RmMember> Members { get; set; }
    public required RmProject? Parent { get; set; }
    public required List<RmProject> Children { get; set; }
}

public partial class RmRole
{
    public required List<RmMemberRole> MemberRoles { get; set; }
}
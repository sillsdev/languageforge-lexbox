using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LexData.Entities;

public class ProjectUsers : EntityBase
{
    public required Guid UserId { get; set; }
    public required Guid ProjectId { get; set; }
    public required ProjectRole Role { get; set; }
    public required User User { get; set; }
    public required Project Project { get; set; }
}

public class ProjectUsersEntityConfiguration : EntityBaseConfiguration<ProjectUsers>
{
}
using LexCore;
using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace LexData;

public class SeedingData
{
    public static readonly Guid TestAdminId = new("cf430ec9-e721-450a-b6a1-9a853212590b");
    private readonly LexBoxDbContext _lexBoxDbContext;
    private readonly IOptions<DbConfig> _dbConfig;

    public SeedingData(LexBoxDbContext lexBoxDbContext, IOptions<DbConfig> dbConfig)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _dbConfig = dbConfig;
    }

    public async Task SeedIfNoUsers(CancellationToken cancellationToken = default)
    {
        if (await _lexBoxDbContext.Users.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        await SeedDatabase(cancellationToken);
    }

    private const string PwSalt = "password-salt";

    public async Task SeedDatabase(CancellationToken cancellationToken = default)
    {
        //NOTE: When seeding make sure you provide a constant Id like I have done here,
        // this will allow us to call seed multiple times without creating new entities each time.
        if (string.IsNullOrEmpty(_dbConfig.Value.DefaultSeedUserPassword))
            throw new Exception("DefaultSeedUserPassword is not set");
        var passwordHash = PasswordHashing.HashPassword(_dbConfig.Value.DefaultSeedUserPassword, PwSalt, false);
        _lexBoxDbContext.Attach(new User
        {
            Id = TestAdminId,
            Email = "admin@test.com",
            Name = "Test Admin",
            Username = "admin",
            Salt = PwSalt,
            PasswordHash = passwordHash,
            IsAdmin = true,
            EmailVerified = true,
            CanCreateProjects = true,
        });

        _lexBoxDbContext.Attach(new User
        {
            Id = new Guid("79198d79-3f69-4de5-914f-96c336e58f94"),
            Email = "user@test.com",
            Name = "Test User",
            Username = "user",
            Salt = PwSalt,
            PasswordHash = passwordHash,
            IsAdmin = false,
            EmailVerified = false,
            CanCreateProjects = false,
        });

        _lexBoxDbContext.Attach(new Project
        {
            Id = new Guid("0ebc5976-058d-4447-aaa7-297f8569f968"),
            Name = "Sena 3",
            Code = "sena-3",
            Type = ProjectType.FLEx,
            MigrationStatus = ProjectMigrationStatus.Migrated,
            ProjectOrigin = ProjectMigrationStatus.Migrated,
            LastCommit = DateTimeOffset.UtcNow,
            RetentionPolicy = RetentionPolicy.Dev,
            Users = new()
            {
                new()
                {
                    Id = new Guid("4605acee-da24-4e50-ba34-73aa5708a6fc"),
                    Role = ProjectRole.Manager,
                    User = new()
                    {
                        Id = new Guid("703701a8-005c-4747-91f2-ac7650455118"),
                        Email = "manager@test.com",
                        Name = "Test Manager",
                        Username = "manager",
                        IsAdmin = false,
                        Salt = PwSalt,
                        PasswordHash = passwordHash,
                        EmailVerified = true,
                        CanCreateProjects = true,
                    }
                },
                new()
                {
                    Id = new Guid("0b505d9d-e15f-4e5f-beef-40e2b8bebb41"),
                    Role = ProjectRole.Editor,
                    User = new()
                    {
                        Id = new Guid("6dc9965b-4021-4606-92df-133fcce75fcb"),
                        Email = "editor@test.com",
                        Name = "Test Editor",
                        Username = "editor",
                        IsAdmin = false,
                        Salt = PwSalt,
                        PasswordHash = passwordHash,
                        EmailVerified = true,
                        CanCreateProjects = false
                    }
                },
            }
        });
        _lexBoxDbContext.Attach(new Project
        {
            Id = new Guid("9e972940-8a8e-4b29-a609-bdc2f93b3507"),
            Name = "Elawa",
            Description = "Eastern Lawa project",
            Code = "elawa-dev-flex",
            Type = ProjectType.FLEx,
            MigrationStatus = ProjectMigrationStatus.Migrated,
            ProjectOrigin = ProjectMigrationStatus.Migrated,
            LastCommit = DateTimeOffset.UtcNow,
            RetentionPolicy = RetentionPolicy.Dev,
            Users = new()
        });

        foreach (var entry in _lexBoxDbContext.ChangeTracker.Entries())
        {
            var exists = await entry.GetDatabaseValuesAsync(cancellationToken) is not null;
            entry.State = exists ? EntityState.Modified : EntityState.Added;
        }

        await _lexBoxDbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task CleanUpSeedData()
    {
        await _lexBoxDbContext.Users.Where(u => u.Salt == PwSalt).ExecuteDeleteAsync();
        await _lexBoxDbContext.Projects.Where(p => p.Code == "sena-3").ExecuteDeleteAsync();
    }
}

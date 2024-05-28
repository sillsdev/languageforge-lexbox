using LexCore;
using LexCore.Entities;
using LexData.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace LexData;

public class SeedingData(LexBoxDbContext lexBoxDbContext, IOptions<DbConfig> dbConfig, IHostEnvironment environment, IOpenIddictApplicationManager? applicationManager = null)
{
    public static readonly Guid TestAdminId = new("cf430ec9-e721-450a-b6a1-9a853212590b");
    public static readonly Guid QaAdminId = new("99b00c58-0dc7-4fe4-b6f2-c27b828811e0");
    private static readonly Guid MangerId = new Guid("703701a8-005c-4747-91f2-ac7650455118");
    private static readonly Guid EditorId = new Guid("6dc9965b-4021-4606-92df-133fcce75fcb");

    public async Task SeedIfNoUsers(CancellationToken cancellationToken = default)
    {
        await SeedOpenId(cancellationToken);
        if (await lexBoxDbContext.Users.CountAsync(cancellationToken) > 0)
        {
            return;
        }

        await SeedUserData(cancellationToken);
    }

    public async Task SeedDatabase(CancellationToken cancellationToken = default)
    {
        await SeedOpenId(cancellationToken);
        await SeedUserData(cancellationToken);
    }

    private const string PwSalt = "password-salt";

    private async Task SeedUserData(CancellationToken cancellationToken = default)
    {
        if (environment.IsProduction()) return;
        //NOTE: When seeding make sure you provide a constant Id like I have done here,
        // this will allow us to call seed multiple times without creating new entities each time.
        if (string.IsNullOrEmpty(dbConfig.Value.DefaultSeedUserPassword))
            throw new Exception("DefaultSeedUserPassword is not set");
        var passwordHash = PasswordHashing.HashPassword(dbConfig.Value.DefaultSeedUserPassword, PwSalt, false);
        lexBoxDbContext.Attach(new User
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
        if (!string.IsNullOrEmpty(dbConfig.Value.QaAdminEmail))
        {
            lexBoxDbContext.Attach(new User
            {
                Id = QaAdminId,
                Email = dbConfig.Value.QaAdminEmail,
                Name = "Qa Admin",
                Username = null,
                Salt = PwSalt,
                PasswordHash = passwordHash,
                IsAdmin = true,
                EmailVerified = true,
                CanCreateProjects = true
            });
        }

        lexBoxDbContext.Attach(new User
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

        lexBoxDbContext.Attach(new Project
        {
            Id = new Guid("0ebc5976-058d-4447-aaa7-297f8569f968"),
            Name = "Sena 3",
            Code = "sena-3",
            Type = ProjectType.FLEx,
            ProjectOrigin = ProjectMigrationStatus.Migrated,
            LastCommit = DateTimeOffset.UtcNow,
            RetentionPolicy = RetentionPolicy.Dev,
            FlexProjectMetadata = new FlexProjectMetadata
            {
                LexEntryCount = -1
            },
            IsConfidential = null,
            Users = new()
            {
                new()
                {
                    Id = new Guid("4605acee-da24-4e50-ba34-73aa5708a6fc"),
                    Role = ProjectRole.Manager,
                    User = new()
                    {
                        Id = MangerId,
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
                        Id = EditorId,
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
        lexBoxDbContext.Attach(new Project
        {
            Id = new Guid("9e972940-8a8e-4b29-a609-bdc2f93b3507"),
            Name = "Elawa",
            Description = "Eastern Lawa project",
            Code = "elawa-dev-flex",
            Type = ProjectType.FLEx,
            ProjectOrigin = ProjectMigrationStatus.Migrated,
            LastCommit = DateTimeOffset.UtcNow,
            RetentionPolicy = RetentionPolicy.Dev,
            IsConfidential = false,
            Users = [],
        });

        lexBoxDbContext.Attach(new Organization
        {
            Id = new Guid("292c80e6-a815-4cd1-9ea2-34bd01274de6"),
            Name = "Test Org",
            Members =
            [
                new OrgMember
                {
                    Id = new Guid("d8e4fb61-6a39-421b-b852-4bdba658d345"), Role = OrgRole.Admin, UserId = MangerId,
                },
                new OrgMember
                {
                    Id = new Guid("1f8bbfd2-1502-456c-94ee-c982650ba325"), Role = OrgRole.User, UserId = EditorId,
                }
            ]
        });

        foreach (var entry in lexBoxDbContext.ChangeTracker.Entries())
        {
            var exists = await entry.GetDatabaseValuesAsync(cancellationToken) is not null;
            entry.State = exists ? EntityState.Modified : EntityState.Added;
        }

        await lexBoxDbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task SeedOpenId(CancellationToken cancellationToken = default)
    {
        if (applicationManager is null) return;
        const string clientId = "becf2856-0690-434b-b192-a4032b72067f";
        if (await applicationManager.FindByClientIdAsync(clientId, cancellationToken) is null)
        {
            await applicationManager.CreateAsync(new OpenIddictApplicationDescriptor
                {
                    ClientId = clientId,//must be guid for MSAL
                    ClientType = OpenIddictConstants.ClientTypes.Public,
                    ApplicationType = OpenIddictConstants.ApplicationTypes.Web,
                    DisplayName = "Oidc Debugger",
                    //explicit requires the user to consent, Implicit does not, External requires an admin to approve, not currently supported
                    ConsentType = OpenIddictConstants.ConsentTypes.Explicit,
                    Permissions =
                    {
                        OpenIddictConstants.Permissions.Endpoints.Authorization,
                        OpenIddictConstants.Permissions.Endpoints.Token,
                        OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode,
                        OpenIddictConstants.Permissions.GrantTypes.RefreshToken,
                        OpenIddictConstants.Permissions.ResponseTypes.Code,
                        OpenIddictConstants.Permissions.Scopes.Email,
                        OpenIddictConstants.Permissions.Scopes.Profile
                    },
                    RedirectUris = { new Uri("https://oidcdebugger.com/debug")}
                },
                cancellationToken);
        }
    }

    public async Task CleanUpSeedData()
    {
        await lexBoxDbContext.Users.Where(u => u.Salt == PwSalt).ExecuteDeleteAsync();
        await lexBoxDbContext.Projects.Where(p => p.Code == "sena-3").ExecuteDeleteAsync();
    }
}

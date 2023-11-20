using LexCore.Entities;
using LexData;
using LexData.Redmine;
using Microsoft.EntityFrameworkCore;

namespace LexBoxApi.Services;

public class MySqlMigrationService
{
    private readonly RedmineDbContext _redminePublicDbContext;
    private readonly RedmineDbContext _privateRedmineDbContext;

    private readonly LexBoxDbContext _lexBoxDbContext;

    public MySqlMigrationService(LexBoxDbContext lexBoxDbContext,
        PublicRedmineDbContext redminePublicDbContext,
        PrivateRedmineDbContext privateRedmineDbContext)
    {
        _lexBoxDbContext = lexBoxDbContext;
        _redminePublicDbContext = redminePublicDbContext;
        _privateRedmineDbContext = privateRedmineDbContext;
    }

    public async Task<int> MigrateData(bool dryRun = true)
    {
        var now = DateTimeOffset.UtcNow;
        await using var transaction = await _lexBoxDbContext.Database.BeginTransactionAsync();
        await Migrate(_redminePublicDbContext, now);
        if (!dryRun) await _lexBoxDbContext.SaveChangesAsync();
        await Migrate(_privateRedmineDbContext, now);
        if (!dryRun)
        {
            await _lexBoxDbContext.SaveChangesAsync();
            await transaction.CommitAsync();
        }

        return _lexBoxDbContext.ChangeTracker.Entries().Count();
    }

    private async Task Migrate(RedmineDbContext dbContext, DateTimeOffset now)
    {
        var existingUsersByEmail =
            await _lexBoxDbContext.Users.ToDictionaryAsync(u => u.Email, u => u, StringComparer.OrdinalIgnoreCase);
        var existingUsersByLogin = existingUsersByEmail.Values.Where(u => !string.IsNullOrEmpty(u.Username))
            .ToDictionary(u => (string) u.Username, u => u);
        var projects = await dbContext.Projects.ToArrayAsync();
        //filter out empty login because there's some default redmine accounts without a login
        var users = await dbContext.Users.Where(u => u.Login != "").Include(u => u.EmailAddresses)
            .ToArrayAsync();
        await dbContext.Members.Include(p => p.Role).ToArrayAsync();
        await dbContext.Roles.ToArrayAsync();
        var migrationStatus = dbContext is PublicRedmineDbContext
            ? ProjectMigrationStatus.PublicRedmine
            : ProjectMigrationStatus.PrivateRedmine;
        var projectIdToGuid = projects.ToDictionary(p => p.Id, p => Guid.NewGuid());
        _lexBoxDbContext.Projects.AddRange(projects.Select(rmProject =>
            MigrateProject(rmProject, now, migrationStatus, projectIdToGuid)));
        _lexBoxDbContext.Users.AddRange(users.Select(rmUser => MigrateUser(rmUser, projectIdToGuid, now, existingUsersByEmail, existingUsersByLogin))
            .OfType<User>());
    }

    private static User? MigrateUser(RmUser rmUser,
        Dictionary<int, Guid> projectIdToGuid,
        DateTimeOffset now,
        Dictionary<string, User> existingUsersByEmail,
        Dictionary<string, User> existingUsersByLogin)
    {
        var userProjects = rmUser.ProjectMembership?.Where(m => m.Project is not null).Select(m => new ProjectUsers
        {
            ProjectId = projectIdToGuid[m.ProjectId],
            Role = m.Role.Role.Name switch
            {
                "Manager" => ProjectRole.Manager,
                "Contributor" => ProjectRole.Editor,
                _ => ProjectRole.Unknown
            },
            CreatedDate = m.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = m.CreatedOn?.ToUniversalTime() ?? now
        }).ToList() ?? new List<ProjectUsers>();
        var email = rmUser.EmailAddresses.FirstOrDefault()?.Address;
        if (email is null) throw new Exception("no email for user id: " + rmUser.Login);
        if (existingUsersByEmail.TryGetValue(email, out var user) ||
            existingUsersByLogin.TryGetValue(rmUser.Login, out user))
        {
            //modify existing user to merge users from public and private
            user.Projects.AddRange(userProjects);
            if (rmUser.Admin)
            {
                user.IsAdmin = true;
            }

            if (user.LastActive < rmUser.LastLoginOn)
            {
                user.LastActive = rmUser.LastLoginOn?.ToUniversalTime() ?? default(DateTimeOffset);
            }
            if (user.CreatedDate > rmUser.CreatedOn)
            {
                user.CreatedDate = rmUser.CreatedOn?.ToUniversalTime() ?? default(DateTimeOffset);
            }
            if (user.UpdatedDate < rmUser.UpdatedOn)
            {
                user.UpdatedDate = rmUser.UpdatedOn?.ToUniversalTime() ?? default(DateTimeOffset);
            }
            if (!user.EmailVerified && rmUser.Status != 2 && user.Email.Equals(email, StringComparison.OrdinalIgnoreCase))
            {
                user.EmailVerified = true;
            }
            if (!user.Locked && rmUser.Status == 3)
            {
                user.Locked = true;
            }
            if (user.LocalizationCode == User.DefaultLocalizationCode && !string.IsNullOrEmpty(rmUser.Language))
            {
                user.LocalizationCode = rmUser.Language;
            }

            if (!user.CanCreateProjects && userProjects.Any(p => p.Role == ProjectRole.Manager))
            {
                user.CanCreateProjects = true;
            }

            //a new user was not added
            return null;
        }

        return new User
        {
            CreatedDate = rmUser.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmUser.UpdatedOn?.ToUniversalTime() ?? now,
            LastActive = rmUser.LastLoginOn?.ToUniversalTime() ?? default(DateTimeOffset),
            Username = rmUser.Login,
            LocalizationCode = rmUser.Language ?? User.DefaultLocalizationCode,
            Email = email,
            Name = rmUser.Firstname + " " + rmUser.Lastname,
            IsAdmin = rmUser.Admin,
            Salt = rmUser.Salt ?? "",
            PasswordHash = rmUser.HashedPassword,
            EmailVerified = rmUser.Status != 2,
            Locked = rmUser.Status == 3,
            Projects = userProjects,
            CanCreateProjects = userProjects.Any(p => p.Role == ProjectRole.Manager)
        };
    }

    private static Project MigrateProject(RmProject rmProject,
        DateTimeOffset now,
        ProjectMigrationStatus migrationStatus,
        Dictionary<int, Guid> projectIdToGuid)
    {
        return new Project
        {
            CreatedDate = rmProject.CreatedOn?.ToUniversalTime() ?? now,
            UpdatedDate = rmProject.UpdatedOn?.ToUniversalTime() ?? now,
            MigrationStatus = migrationStatus,
            ProjectOrigin = migrationStatus,
            Name = rmProject.Name,
            Code = rmProject.Identifier ?? throw new Exception("no code for project id" + rmProject.Id),
            Description = rmProject.Description,
            Type = rmProject.Identifier!.EndsWith("-flex") ? ProjectType.FLEx : ProjectType.Unknown,
            RetentionPolicy =
                rmProject.Identifier.Contains("test") ? RetentionPolicy.Test : RetentionPolicy.Unknown,
            LastCommit = null,
            Id = projectIdToGuid[rmProject.Id],
            ParentId = rmProject.ParentId switch
            {
                null => null,
                _ => projectIdToGuid[rmProject.ParentId.Value]
            },
            Users = new List<ProjectUsers>()
        };
    }

    public static bool IsMySqlMigrationRequest(string[] args)
    {
        return args is ["migrate-mysql"];
    }

    public static async Task RunMySqlMigrationRequest(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddLogging();
        builder.Services.AddSingleton<MySqlMigrationService>();
        builder.Services.AddLexData(false, ServiceLifetime.Singleton);
        var host = builder.Build();
        await host.Services.GetRequiredService<LexBoxDbContext>().Database.EnsureCreatedAsync();
        await host.Services.GetRequiredService<MySqlMigrationService>().MigrateData(false);
    }
}

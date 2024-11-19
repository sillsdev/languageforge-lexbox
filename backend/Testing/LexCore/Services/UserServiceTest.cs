using LexBoxApi.Services;
using LexBoxApi.Services.Email;
using LexCore.Auth;
using LexCore.Entities;
using LexData;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Testing.Fixtures;
using FluentAssertions;

namespace Testing.LexCore.Services;

[Collection(nameof(TestingServicesFixture))]
public class UserServiceTest : IAsyncLifetime
{
    private readonly UserService _userService;

    private readonly LexBoxDbContext _lexBoxDbContext;
    private List<Project> ManagedProjects { get; } = [];
    private List<User> ManagedUsers { get; } = [];

    // Users created for this test
    private User? Robin { get; set; }
    private User? John { get; set; }
    private User? Marian { get; set; }
    private User? Tuck { get; set; }
    private User? Sheriff { get; set; }
    // Projects created for this test
    private Project? Sherwood { get; set; }
    private Project? Nottingham { get; set; }

    public UserServiceTest(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddScoped<IEmailService>(_ => Mock.Of<IEmailService>());
            s.AddScoped<UserService>();
        });
        _userService = serviceProvider.GetRequiredService<UserService>();
        _lexBoxDbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    public Task InitializeAsync()
    {
        Robin = CreateUser("Robin Hood");
        John = CreateUser("Little John");
        Marian = CreateUser("Maid Marian");
        Tuck = CreateUser("Friar Tuck");
        Sheriff = CreateUser("Sheriff of Nottingham");

        Nottingham = CreateProject([Sheriff.Id], [Marian.Id, Tuck.Id]);
        Sherwood = CreateConfidentialProject([Robin.Id, Marian.Id], [John.Id, Tuck.Id]);

        return _lexBoxDbContext.SaveChangesAsync();
    }

    public Task DisposeAsync()
    {
        foreach (var project in ManagedProjects)
        {
            _lexBoxDbContext.Remove(project);
        }
        foreach (var user in ManagedUsers)
        {
            _lexBoxDbContext.Remove(user);
        }
        return _lexBoxDbContext.SaveChangesAsync();
    }

    [Fact]
    public async Task ManagerCanSeeAllUsersEvenInConfidentialProjects()
    {
        var authUser = new LexAuthUser(Robin!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        users.Should().BeEquivalentTo([Robin, Marian, John, Tuck]);
    }

    [Fact]
    public async Task NonManagerCanNotSeeUsersInConfidentialProjects()
    {
        var authUser = new LexAuthUser(John!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        users.Should().BeEmpty();
    }

    [Fact]
    public async Task ManagerOfOneProjectAndMemberOfAnotherPublicProjectCanSeeUsersInBoth()
    {
        var authUser = new LexAuthUser(Marian!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        users.Should().BeEquivalentTo([Robin, Marian, John, Tuck, Sheriff]);
    }

    [Fact]
    public async Task ManagerOfOneProjectAndMemberOfAnotherConfidentialProjectCanNotSeeUsersInConfidentialProject()
    {
        try
        {
            // Sheriff tries to sneak into Sherwood...
            await AddUserToProject(Sherwood!, Sheriff!);
            // ... but can still only see the users in Nottingham
            var authUser = new LexAuthUser(Sheriff!);
            var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
            users.Should().BeEquivalentTo([Sheriff, Marian, Tuck]);
        }
        finally
        {
            await RemoveUserFromProject(Sherwood!, Sheriff!);
        }
    }

    private User CreateUser(string name)
    {
        var email = name.ToLowerInvariant().Replace(' ', '_') + "@example.com";
        var user = new User
        {
            Name = name,
            Email = email,
            CanCreateProjects = true,
            EmailVerified = true,
            IsAdmin = name.Contains("Admin"),
            PasswordHash = "",
            Salt = ""
        };
        _lexBoxDbContext.Add(user);
        ManagedUsers.Add(user);
        return user; // Caller must call SaveChanges after all users and projects are added
    }

    private Project CreateProject(IEnumerable<Guid> managers, IEnumerable<Guid> members, bool isConfidential = false)
    {
        var config = Testing.Services.Utils.GetNewProjectConfig();
        var project = new Project
        {
            Name = config.Name,
            Code = config.Code,
            IsConfidential = isConfidential,
            LastCommit = null,
            Organizations = [],
            Users = [],
            RetentionPolicy = RetentionPolicy.Test,
            Type = ProjectType.FLEx,
            Id = config.Id,
        };
        project.Users.AddRange(managers.Select(userId => new ProjectUsers { UserId = userId, Role = ProjectRole.Manager }));
        project.Users.AddRange(members.Select(userId => new ProjectUsers { UserId = userId, Role = ProjectRole.Editor }));
        _lexBoxDbContext.Add(project);
        ManagedProjects.Add(project);
        return project; // Caller must call SaveChanges after all users and projects are added
    }

    private Project CreateConfidentialProject(IEnumerable<Guid> managers, IEnumerable<Guid> members)
    {
        return CreateProject(managers, members, true);
    }

    private async Task AddUserToProject(Project project, User user, ProjectRole role = ProjectRole.Editor)
    {
        var pu = project.Users.FirstOrDefault(pu => pu.UserId == user.Id);
        if (pu is null) project.Users.Add(new ProjectUsers { UserId = user.Id, Role = role });
        else pu.Role = role;
        await _lexBoxDbContext.SaveChangesAsync();
    }

    private async Task RemoveUserFromProject(Project project, User user)
    {
        var pu = project.Users.FirstOrDefault(pu => pu.UserId == user.Id);
        if (pu is not null) project.Users.Remove(pu);
        await _lexBoxDbContext.SaveChangesAsync();
    }
}

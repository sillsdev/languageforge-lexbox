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
[Trait("Category", "RequiresDb")]
public class UserServiceTest : IAsyncLifetime
{
    private readonly UserService _userService;

    private readonly LexBoxDbContext _lexBoxDbContext;
    private List<Project> ManagedProjects { get; } = [];
    private List<User> ManagedUsers { get; } = [];
    private List<Organization> ManagedOrgs { get; } = [];

    // Users created for this test
    private User? Robin { get; set; }
    private User? John { get; set; }
    private User? Alan { get; set; }
    private User? Marian { get; set; }
    private User? Bishop { get; set; }
    private User? Tuck { get; set; }
    private User? Sheriff { get; set; }
    private User? Guy { get; set; }
    // Projects created for this test
    private Project? Sherwood { get; set; }
    private Project? Nottingham { get; set; }
    // Orgs created for this test
    private Organization? Outlaws { get; set; }
    private Organization? LawEnforcement { get; set; }
    private Organization? Church { get; set; }

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
        Alan = CreateUser("Alan a Dale");
        Marian = CreateUser("Maid Marian");
        Bishop = CreateUser("Bishop of Hereford");
        Tuck = CreateUser("Friar Tuck");
        Sheriff = CreateUser("Sheriff of Nottingham");
        Guy = CreateUser("Guy of Gisbourne");

        Nottingham = CreateProject([Sheriff.Id], [Marian.Id, Tuck.Id]);
        Sherwood = CreateConfidentialProject([Robin.Id, Marian.Id], [John.Id, Alan.Id, Tuck.Id]);

        Outlaws = CreateOrg([Robin.Id], [John.Id]); // Alan a Dale should *NOT* be in this org
        LawEnforcement = CreateOrg([Sheriff.Id], [Guy.Id]);
        Church = CreateOrg([Bishop.Id], [Tuck.Id]);

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
        foreach (var org in ManagedOrgs)
        {
            _lexBoxDbContext.Remove(org);
        }
        return _lexBoxDbContext.SaveChangesAsync();
    }

    private void UserListShouldBe(IEnumerable<User> actual, IEnumerable<User?> expected)
    {
        var actualNames = actual.Select(u => u.Name);
        var expectedNames = expected.Select(u => u?.Name ?? "<null name>");
        actualNames.Should().BeEquivalentTo(expectedNames, options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ManagerCanSeeAllUsersEvenInConfidentialProjects()
    {
        // Robin Hood is in Outlaws org (admin) and Sherwood project (private, manager)
        var authUser = new LexAuthUser(Robin!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // John, who is in both the Outlaws org (user) and Sherwood project (member) is not duplicated
        UserListShouldBe(users, [Robin, Marian, John, Alan, Tuck]);
    }

    [Fact]
    public async Task NonManagerCanNotSeeUsersInConfidentialProjects()
    {
        // Little John is in Outlaws org (user) and Sherwood project (private, member)
        var authUser = new LexAuthUser(John!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // John can see Robin because he shares an org, but not Marian even though she's a manager of the Sherwood project
        UserListShouldBe(users, [Robin, John]);
    }

    [Fact]
    public async Task ManagerOfOneProjectAndMemberOfAnotherPublicProjectCanSeeUsersInBoth()
    {
        // Maid Marian is in no orgs and two projects: Sherwood (private, manager) and Nottingham (public, member)
        var authUser = new LexAuthUser(Marian!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // Marian can see everyone in both projects; Tuck is not duplicated despite being in both projects
        UserListShouldBe(users, [Robin, Marian, John, Alan, Tuck, Sheriff]);
    }

    [Fact]
    public async Task ManagerOfOneProjectAndMemberOfAnotherConfidentialProjectCanNotSeeUsersInConfidentialProject()
    {
        // Sheriff of Nottingham is in LawEnforcement org (admin) and Nottingham project (pulbic, manager)
        try
        {
            // Sheriff tries to sneak into Sherwood...
            await AddUserToProject(Sherwood!, Sheriff!);
            // ... but can still only see the users in Nottingham and LawEnforcement
            var authUser = new LexAuthUser(Sheriff!);
            var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
            UserListShouldBe(users, [Sheriff, Guy, Marian, Tuck]);
        }
        finally
        {
            await RemoveUserFromProject(Sherwood!, Sheriff!);
        }
    }

    [Fact]
    public async Task OrgAdminsInNoProjectsCanSeeOnlyTheirOrg()
    {
        // Bishop of Hereford is in Church org (admin) but no projects
        var authUser = new LexAuthUser(Bishop!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // Bishop can only see members of Church org
        UserListShouldBe(users, [Bishop, Tuck]);
    }

    [Fact]
    public async Task OrgMembersInNoProjectsCanSeeOnlyTheirOrg()
    {
        // Guy of Gisborne is in LawEnforcement org (user) but no projects
        var authUser = new LexAuthUser(Guy!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // Guy can only see members of LawEnforcement org
        UserListShouldBe(users, [Sheriff, Guy]);
    }

    [Fact]
    public async Task OrgAndProjectMembersCanSeeFellowOrgMembersAndFellowPublicProjectMembersButNotFellowPrivateProjectMembers()
    {
        // Friar Tuck is in Church org (user) and two projects: Nottingham (public, member) and Sherwood (private, member)
        var authUser = new LexAuthUser(Tuck!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // Tuck can see everyone in Church and Nottingham, but nobody in Sherwood because it's private — though he can see Marian because he shares a public project with her
        UserListShouldBe(users, [Bishop, Tuck, Sheriff, Marian]);
    }

    [Fact]
    public async Task MemberOfOnePrivateProjectButNoOrgsCanOnlySeeHimself()
    {
        // Alan a Dale is in Sherwood project (private, member) but no orgs
        var authUser = new LexAuthUser(Alan!);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        // Alan can see himself in the Sherwood project, but nobody else because it's private
        UserListShouldBe(users, [Alan]);
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

    private Organization CreateOrg(IEnumerable<Guid> managers, IEnumerable<Guid> members)
    {
        var id = Guid.NewGuid();
        var shortId = id.ToString().Split("-")[0];
        var org = new Organization
        {
            Name = shortId,
            Members = [],
            Projects = [],
        };
        org.Members.AddRange(managers.Select(userId => new OrgMember { UserId = userId, Role = OrgRole.Admin }));
        org.Members.AddRange(members.Select(userId => new OrgMember { UserId = userId, Role = OrgRole.User }));
        _lexBoxDbContext.Add(org);
        ManagedOrgs.Add(org);
        return org;
    }
}

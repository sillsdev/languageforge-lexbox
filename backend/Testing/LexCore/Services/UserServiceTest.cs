using LexBoxApi.Auth;
using LexBoxApi.Services;
using LexBoxApi.Services.Email;
using LexCore.Auth;
using LexCore.Entities;
using LexCore.ServiceInterfaces;
using LexData;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Npgsql;
using Shouldly;
using Testing.Fixtures;

namespace Testing.LexCore.Services;

[Collection(nameof(TestingServicesFixture))]
public class UserServiceTest
{
    private readonly UserService _userService;

    private LexBoxDbContext _lexBoxDbContext;

    public UserServiceTest(TestingServicesFixture testing)
    {
        var serviceProvider = testing.ConfigureServices(s =>
        {
            s.AddScoped<IEmailService>(_ => Mock.Of<IEmailService>());
            s.AddScoped<IHttpContextAccessor>(_ => Mock.Of<IHttpContextAccessor>());
            s.AddSingleton<IMemoryCache>(_ => Mock.Of<IMemoryCache>());
            s.AddScoped<UserService>();
            s.AddScoped<ProjectService>();
        });
        _userService = serviceProvider.GetRequiredService<UserService>();
        _lexBoxDbContext = serviceProvider.GetRequiredService<LexBoxDbContext>();
    }

    public async Task<Project> CreateProject(bool isConfidential = false, Guid? managerId = null)
    {
        // TODO: Return some kind of disposable fixture with a .Project property, which
        // deletes the project when disposed and can also add users with a single method call
        var config = Testing.Services.Utils.GetNewProjectConfig(isConfidential: isConfidential);
        var project = new Project
        {
            Name = config.Name,
            Code = config.Code,
            IsConfidential = config.IsConfidential,
            LastCommit = null,
            Organizations = [],
            Users = [],
            RetentionPolicy = RetentionPolicy.Test,
            Type = ProjectType.FLEx,
            Id = config.Id,
        };
        if (managerId is Guid id)
        {
            project.Users.Add(new ProjectUsers { ProjectId = project.Id, UserId = id, Role = ProjectRole.Manager });
        }
        _lexBoxDbContext.Add(project);
        await _lexBoxDbContext.SaveChangesAsync();
        return project;
    }

    [Fact]
    public async Task ManagerCanSeeAllUsersEvenInConfidentialProjects()
    {
        var manager = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "manager@test.com");
        manager.ShouldNotBeNull();
        var privateProject = await CreateProject(true, manager.Id);
        try {
            privateProject.ShouldNotBeNull();
            var qaAdmin = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "qa@test.com");
            qaAdmin.ShouldNotBeNull();
            privateProject.Users.Add(new ProjectUsers() { UserId = qaAdmin.Id, Role = ProjectRole.Editor });
            _lexBoxDbContext.SaveChanges();
            var authUser = new LexAuthUser(manager);
            var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
            users.ShouldNotBeEmpty();
            users.ShouldContain(u => u.Id == qaAdmin.Id);
        }
        finally
        {
            _lexBoxDbContext.Projects.Remove(privateProject);
            await _lexBoxDbContext.SaveChangesAsync();
        }
    }

    [Fact]
    public async Task NonManagerCanNotSeeUsersInConfidentialProjects()
    {
        var manager = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "manager@test.com");
        manager.ShouldNotBeNull();
        var editor = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "editor@test.com");
        editor.ShouldNotBeNull();
        var privateProject = await CreateProject(true, manager.Id);
        try {
            privateProject.ShouldNotBeNull();
            var qaAdmin = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "qa@test.com");
            qaAdmin.ShouldNotBeNull();
            privateProject.Users.Add(new ProjectUsers() { UserId = qaAdmin.Id, Role = ProjectRole.Editor });
            _lexBoxDbContext.SaveChanges();
            var authUser = new LexAuthUser(editor);
            var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
            users.ShouldNotContain(u => u.Id == qaAdmin.Id);
        }
        finally
        {
            _lexBoxDbContext.Projects.Remove(privateProject);
            await _lexBoxDbContext.SaveChangesAsync();
        }
    }
}

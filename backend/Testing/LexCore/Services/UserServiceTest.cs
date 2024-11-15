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

    private readonly LexBoxDbContext _lexBoxDbContext;

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

    [Fact]
    public async Task ManagerCanSeeAllUsersEvenInConfidentialProjects()
    {
        var manager = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "manager@test.com");
        manager.ShouldNotBeNull();
        await using var privateProject = await TempProjectWithoutRepo.Create(_lexBoxDbContext, true, manager.Id);
        privateProject.Project.ShouldNotBeNull();
        var qaAdmin = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "qa@test.com");
        qaAdmin.ShouldNotBeNull();
        privateProject.Project.Users.Add(new ProjectUsers() { UserId = qaAdmin.Id, Role = ProjectRole.Editor });
        await _lexBoxDbContext.SaveChangesAsync();
        var authUser = new LexAuthUser(manager);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        users.ShouldNotBeEmpty();
        users.ShouldContain(u => u.Id == qaAdmin.Id);
    }

    [Fact]
    public async Task NonManagerCanNotSeeUsersInConfidentialProjects()
    {
        var manager = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "manager@test.com");
        manager.ShouldNotBeNull();
        var editor = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "editor@test.com");
        editor.ShouldNotBeNull();
        await using var privateProject = await TempProjectWithoutRepo.Create(_lexBoxDbContext, true, manager.Id);
        privateProject.Project.ShouldNotBeNull();
        var qaAdmin = await _lexBoxDbContext.Users.Include(u => u.Organizations).Include(u => u.Projects).FirstOrDefaultAsync(user => user.Email == "qa@test.com");
        qaAdmin.ShouldNotBeNull();
        privateProject.Project.Users.Add(new ProjectUsers() { UserId = qaAdmin.Id, Role = ProjectRole.Editor });
        await _lexBoxDbContext.SaveChangesAsync();
        var authUser = new LexAuthUser(editor);
        var users = await _userService.UserQueryForTypeahead(authUser).ToArrayAsync();
        users.ShouldNotContain(u => u.Id == qaAdmin.Id);
    }
}

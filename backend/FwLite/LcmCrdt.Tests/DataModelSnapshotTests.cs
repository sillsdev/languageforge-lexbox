using System.Text.Json;
using FluentAssertions.Execution;
using LcmCrdt.Objects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MiniLcm.Tests.AutoFakerHelpers;
using SIL.Harmony.Changes;
using SIL.Harmony.Core;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public class DataModelSnapshotTests : IAsyncLifetime
{
    private static readonly AutoFaker Faker = new(AutoFakerDefault.Config);

    protected readonly AsyncServiceScope _services;
    private readonly LcmCrdtDbContext _crdtDbContext;
    private CrdtConfig _crdtConfig;
    private CrdtProject _crdtProject;
    private readonly JsonSerializerOptions _jsonSerializerOptions = TestJsonOptions.Harmony();

    public DataModelSnapshotTests()
    {
        _crdtProject = new CrdtProject("sena-3", $"sena-3-{Guid.NewGuid()}.sqlite");
        var services = new ServiceCollection()
            .AddTestLcmCrdtClient(_crdtProject)
            .AddLogging(builder => builder.AddDebug())
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = _services.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContext();
        _crdtConfig = _services.ServiceProvider.GetRequiredService<IOptions<CrdtConfig>>().Value;
    }

    public async Task InitializeAsync()
    {
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        var projectData = new ProjectData("Sena 3", "sena-3", Guid.NewGuid(), null, Guid.NewGuid());
        await CrdtProjectsService.InitProjectDb(_crdtDbContext, projectData);
        _crdtProject.Data = projectData;
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(_crdtProject);
    }

    public async Task DisposeAsync()
    {
        await _crdtDbContext.Database.CloseConnectionAsync();
        await _crdtDbContext.Database.EnsureDeletedAsync();
        await _crdtDbContext.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Fact]
    public async Task VerifyDbModel()
    {
        await Verify(_crdtDbContext.Model.ToDebugString(MetadataDebugStringOptions.LongDefault));
    }

    [Fact]
    public async Task VerifyChangeModels()
    {
        await Verify(_jsonSerializerOptions.GetTypeInfo(typeof(IChange)).PolymorphismOptions);
    }

    [Fact]
    public async Task VerifyIObjectBaseModels()
    {
        await Verify(_jsonSerializerOptions.GetTypeInfo(typeof(IObjectBase)).PolymorphismOptions);
    }

    [Fact]
    public async Task VerifyIObjectWithIdModels()
    {
        await Verify(_jsonSerializerOptions.GetTypeInfo(typeof(IObjectWithId)).PolymorphismOptions);
    }

    [Fact]
    public void VerifyIObjectWithIdsMatchAdapterGetObjectTypeName()
    {
        var types = _jsonSerializerOptions.GetTypeInfo(typeof(IObjectWithId)).PolymorphismOptions?.DerivedTypes ?? [];
        using (new AssertionScope())
        {
            foreach (var jsonDerivedType in types)
            {
                var typeDiscriminator = jsonDerivedType.TypeDiscriminator.Should().BeOfType<string>().Subject;
                var obj = Faker.Generate(jsonDerivedType.DerivedType);
                new MiniLcmCrdtAdapter((IObjectWithId)obj).GetObjectTypeName().Should().Be(typeDiscriminator);
            }
        }
    }
}

using FluentAssertions.Execution;
using LcmCrdt.Objects;
using LcmCrdt.Tests.Mocks;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SIL.Harmony.Changes;
using SIL.Harmony.Entities;
using Soenneker.Utils.AutoBogus;

namespace LcmCrdt.Tests;

public class DataModelSnapshotTests : IAsyncLifetime
{
    protected readonly AsyncServiceScope _services;
    private readonly LcmCrdtDbContext _crdtDbContext;
    private CrdtConfig _crdtConfig;
    public DataModelSnapshotTests()
    {

        var services = new ServiceCollection()
            .AddLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug())
            .RemoveAll(typeof(ProjectContext))
            .AddSingleton<ProjectContext>(new MockProjectContext(new CrdtProject("sena-3", ":memory:")))
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtDbContext = _services.ServiceProvider.GetRequiredService<LcmCrdtDbContext>();
        _crdtConfig = _services.ServiceProvider.GetRequiredService<IOptions<CrdtConfig>>().Value;
    }

    public async Task InitializeAsync()
    {
        await _crdtDbContext.Database.OpenConnectionAsync();
        //can't use ProjectsService.CreateProject because it opens and closes the db context, this would wipe out the in memory db.
        await ProjectsService.InitProjectDb(_crdtDbContext,
            new ProjectData("Sena 3", Guid.NewGuid(), null, Guid.NewGuid()));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().PopulateProjectDataCache();
    }

    public async Task DisposeAsync()
    {
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
        var jsonSerializerOptions = _crdtConfig.JsonSerializerOptions;
        await Verify(jsonSerializerOptions.GetTypeInfo(typeof(IChange)).PolymorphismOptions);
    }

    [Fact]
    public async Task VerifyIObjectBaseModels()
    {
        var jsonSerializerOptions = _crdtConfig.JsonSerializerOptions;
        await Verify(jsonSerializerOptions.GetTypeInfo(typeof(IObjectBase)).PolymorphismOptions);
    }

    [Fact]
    public async Task VerifyIObjectWithIdModels()
    {
        var jsonSerializerOptions = _crdtConfig.JsonSerializerOptions;
        await Verify(jsonSerializerOptions.GetTypeInfo(typeof(IObjectWithId)).PolymorphismOptions);
    }

    [Fact]
    public void VerifyIObjectWithIdsMatchAdapterGetObjectTypeName()
    {
        var faker = new AutoFaker();
        var jsonSerializerOptions = _crdtConfig.JsonSerializerOptions;
        var types = jsonSerializerOptions.GetTypeInfo(typeof(IObjectWithId)).PolymorphismOptions?.DerivedTypes ?? [];
        using (new AssertionScope())
        {
            foreach (var jsonDerivedType in types)
            {
                var typeDiscriminator = jsonDerivedType.TypeDiscriminator.Should().BeOfType<string>().Subject;
                var obj = faker.Generate(jsonDerivedType.DerivedType);
                new MiniLcmCrdtAdapter((IObjectWithId)obj).GetObjectTypeName().Should().Be(typeDiscriminator);
            }
        }
    }
}

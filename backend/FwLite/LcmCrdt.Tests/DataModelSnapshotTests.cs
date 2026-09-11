using SIL.Harmony.Config;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
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
    private LcmCrdtDbContext _crdtDbContext = null!;
    private HarmonyConfig _crdtConfig;
    private readonly JsonSerializerOptions _jsonSerializerOptions = TestJsonOptions.Harmony();

    public DataModelSnapshotTests()
    {
        var services = new ServiceCollection()
            .AddTestLcmCrdtClient()
            .AddLogging(builder => builder.AddDebug())
            .BuildServiceProvider();
        _services = services.CreateAsyncScope();
        _crdtConfig = _services.ServiceProvider.GetRequiredService<IOptions<HarmonyConfig>>().Value;
    }

    public async Task InitializeAsync()
    {
        //unique db path per instance, so CurrentProjectService doesn't think a db has already run migrations
        var crdtProject = await _services.ServiceProvider.GetRequiredService<CrdtProjectsService>()
            .CreateProject(new("Sena 3", "sena-3", DbPath: $"sena-3-{Guid.NewGuid()}.sqlite"));
        await _services.ServiceProvider.GetRequiredService<CurrentProjectService>().SetupProjectContext(crdtProject);
        _crdtDbContext = await _services.ServiceProvider.GetRequiredService<IDbContextFactory<LcmCrdtDbContext>>().CreateDbContextAsync();
    }

    public async Task DisposeAsync()
    {
        await _crdtDbContext.Database.EnsureDeletedAsync();
        await _crdtDbContext.DisposeAsync();
        await _services.DisposeAsync();
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task VerifyDbModel()
    {
        await Verify(_crdtDbContext.Model.ToDebugString(MetadataDebugStringOptions.LongDefault));
    }

    private IEnumerable<JsonDerivedType> GetPolymorphicTypesFor(Type type)
    {
        var polymorphismOptions = _jsonSerializerOptions.GetTypeInfo(type)
            .PolymorphismOptions;
        polymorphismOptions.Should().NotBeNull("type {0} should only be called if it's configured properly", type);
        polymorphismOptions.TypeDiscriminatorPropertyName.Should().Be("$type");
        return polymorphismOptions.DerivedTypes.OrderBy(t => t.DerivedType.FullName);
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task VerifyChangeModels()
    {
        await Verify(LcmCrdtKernel.AllRegisteredChanges()
            //map to JsonDerivedType to match the verification format previously used
            .Select(c => new JsonDerivedType(c.Type, c.Discriminator))
            .OrderBy(j => j.DerivedType.FullName));
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task VerifyIObjectBaseModels()
    {
        await Verify(GetPolymorphicTypesFor(typeof(IObjectBase)));
    }

    [Fact]
    [Trait("Category", "Verified")]
    public async Task VerifyIObjectWithIdModels()
    {
        await Verify(GetPolymorphicTypesFor(typeof(IObjectWithId)));
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

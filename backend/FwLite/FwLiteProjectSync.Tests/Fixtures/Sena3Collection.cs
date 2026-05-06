namespace FwLiteProjectSync.Tests.Fixtures;

/// <summary>
/// Groups every test class that uses <see cref="Sena3Fixture"/> into a single xUnit
/// collection so they share one fixture instance and run serially. Without this, parallel
/// classes each spin up their own fixture and race on the shared <c>./Sena3Fixture/</c>
/// folder during <see cref="Sena3Fixture.InitializeAsync"/>.
/// </summary>
[CollectionDefinition(Name)]
public class Sena3Collection : ICollectionFixture<Sena3Fixture>
{
    public const string Name = nameof(Sena3Collection);
}

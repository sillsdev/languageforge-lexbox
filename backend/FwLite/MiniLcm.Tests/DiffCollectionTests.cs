using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

public class DiffCollectionTests
{
    [Fact]
    public async Task MatchingCollections_NoChangesAreGenerated()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());

        // act
        var (changeCount, diffOperations, replacements) = await Diff([value1, value2], [value1, value2]);

        // assert
        changeCount.Should().Be(0);
        diffOperations.Should().BeEmpty();
        replacements.Should().BeEquivalentTo([(value1, value1), (value2, value2)]);
    }

    [Fact]
    public async Task AddedValues()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());
        var value3 = new TestOrderable(3, Guid.NewGuid());

        // act
        var (changeCount, diffOperations, replacements) = await Diff([value1], [value2, value1, value3]);

        // assert
        changeCount.Should().Be(2);
        replacements.Should().BeEquivalentTo([(value1, value1)]);
        diffOperations.Should().BeEquivalentTo([
            Add(value2, Between(null, value1)),
            Add(value3, Between(value1, null)),
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task RemovedValues()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());
        var value3 = new TestOrderable(3, Guid.NewGuid());

        // act
        var (changeCount, diffOperations, replacements) = await Diff([value2, value1, value3], [value1]);

        // assert
        changeCount.Should().Be(2);
        replacements.Should().BeEquivalentTo([(value1, value1)]);
        diffOperations.Should().BeEquivalentTo([
            Remove(value3),
            Remove(value2),
        ], options => options.WithStrictOrdering());
    }

    [Fact]
    public async Task SwappedValues()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());

        // act
        var (changeCount, diffOperations, replacements) = await Diff([value1, value2], [value2, value1]);

        // assert
        changeCount.Should().Be(1);
        replacements.Should().BeEquivalentTo([(value1, value1), (value2, value2)]);
        diffOperations.Should().BeEquivalentTo([
            Move(value1, Between(value2, null)),
        ]);
    }

    public static IEnumerable<object[]> CollectionDiffTestCaseData()
    {
        var _1 = new TestOrderable(1, Guid.NewGuid());
        var _2 = new TestOrderable(2, Guid.NewGuid());
        var _3 = new TestOrderable(3, Guid.NewGuid());
        var _4 = new TestOrderable(4, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3],
            NewValues = [_3, _2, _1],
            ExpectedOperations = [
                Move(_2, Between(_3, null)),
                Move(_1, Between(_2, null)),
            ],
        }];
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4],
            NewValues = [_1, _4, _2, _3],
            ExpectedOperations = [
                Move(_4, Between(_1, _2)),
            ],
        }];
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4],
            NewValues = [_2, _1, _4, _3],
            ExpectedOperations = [ // When only 4, moving the 2 outsides to middle is represented slightly differently:
                Move(_1, Between(_2, _4)),
                Move(_3, Between(_4, null)),
            ],
        }];

        var _5 = new TestOrderable(5, Guid.NewGuid());
        var _6 = new TestOrderable(6, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4, _5, _6],
            NewValues = [_2, _3, _1, _6, _4, _5],
            ExpectedOperations = [ // When 6+, moving the 2 outsides to middle is represented as such:
                Move(_1, Between(_3, _4)),
                Move(_6, Between(_1, _4)),
            ],
        }];

        var _7 = new TestOrderable(7, Guid.NewGuid());
        var _8 = new TestOrderable(8, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4, _5],
            NewValues = [_6, _8, _4, _2, _7],
            ExpectedOperations = [
                Remove(_5),
                Remove(_3),
                Remove(_1),
                Add(_6, Between(null, _4)), // (not next: _8, because _8 is not "stable")
                Add(_8, Between(_6, _4)),
                Move(_2, Between(_4, null)),
                Add(_7, Between(_2, null)),
            ],
        }];
    }

    [Theory]
    [MemberData(nameof(CollectionDiffTestCaseData))]
    public async Task DiffTests(CollectionDiffTestCase testCase)
    {
        // act
        var (changeCount, diffOperations, replacements) = await Diff(testCase.OldValues, testCase.NewValues);

        // assert
        changeCount.Should().Be(testCase.ExpectedOperations.Count);
        diffOperations.Should().BeEquivalentTo(testCase.ExpectedOperations, options => options.WithStrictOrdering());
    }

    private static async Task<(
        int changeCount,
        List<CollectionDiffOperation> DiffOperations,
        List<(TestOrderable before, TestOrderable after)> Replacements
    )> Diff(TestOrderable[] oldValues, TestOrderable[] newValues)
    {
        var diffApi = new TestOrderableDiffApi(oldValues);
        var changeCount = await DiffCollection.DiffOrderable(oldValues, newValues, diffApi);
        // verify that the operations did in fact transform the old collection into the new collection
        diffApi.Current.Should().BeEquivalentTo(newValues, options => options.WithStrictOrdering());
        var expectedReplacements = oldValues.Join(newValues, o => o.Id, o => o.Id, (o, n) => (o, n)).ToList();
        diffApi.Replacements.Should().BeEquivalentTo(expectedReplacements, options => options.WithStrictOrdering());
        return (changeCount, diffApi.DiffOperations, diffApi.Replacements);
    }

    private static BetweenPosition Between(TestOrderable? previous, TestOrderable? next)
    {
        return Between(previous?.Id, next?.Id);
    }

    private static BetweenPosition Between(Guid? previous = null, Guid? next = null)
    {
        return new BetweenPosition(previous, next);
    }

    private static CollectionDiffOperation Move(TestOrderable value, BetweenPosition between)
    {
        return new CollectionDiffOperation(value, PositionDiffKind.Move, between);
    }

    private static CollectionDiffOperation Add(TestOrderable value, BetweenPosition between)
    {
        return new CollectionDiffOperation(value, PositionDiffKind.Add, between);
    }

    private static CollectionDiffOperation Remove(TestOrderable value)
    {
        return new CollectionDiffOperation(value, PositionDiffKind.Remove);
    }


    public record Entry(Guid Id, string Word);

    private readonly Mock<CollectionDiffApi<Entry, Guid>> _mock;

    public DiffCollectionTests()
    {
        _mock = new Mock<CollectionDiffApi<Entry, Guid>>();
        _mock.Setup(api => api.GetId(It.IsAny<Entry>())).Returns<Entry>(value => value.Id).Verifiable(Times.Once);
    }

    [Fact]
    public async Task Diff_CallsAddForNewRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        await DiffCollection.Diff([], [entry], _mock.Object);
        _mock.Verify(api => api.Add(entry), Times.Once);
    }

    [Fact]
    public async Task Diff_CallsRemoveForMissingRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        await DiffCollection.Diff([entry], [], _mock.Object);
        _mock.Verify(api => api.Remove(entry), Times.Once);
    }

    [Fact]
    public async Task Diff_CallsReplaceForMatchingRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        var updated = entry with { Word = "new" };
        await DiffCollection.Diff([entry], [updated], _mock.Object);
        _mock.Verify(api => api.Replace(entry, updated), Times.Once);
    }

    [Fact]
    public async Task Diff_AddThenUpdate_CallsAddForNewRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        await DiffCollection.DiffAddThenUpdate([], [entry], _mock.Object);
        var sequence = new MockSequence();
        _mock.InSequence(sequence).Setup(api => api.Replace(entry, entry)).ReturnsAsync(1).Verifiable(Times.Once);
        _mock.InSequence(sequence).Setup(api => api.AddAndGet(entry)).ReturnsAsync((1, entry)).Verifiable(Times.Once);
        _mock.VerifyAll();
        _mock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DiffAddThenUpdate_CallsRemoveForMissingRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        await DiffCollection.DiffAddThenUpdate([entry], [], _mock.Object);
        _mock.Verify(api => api.Remove(entry), Times.Once);
    }

    [Fact]
    public async Task DiffAddThenUpdate_CallsReplaceForMatchingRecords()
    {
        var entry = new Entry(Guid.NewGuid(), "test");
        var updated = entry with { Word = "new" };
        await DiffCollection.DiffAddThenUpdate([entry], [updated], _mock.Object);
        _mock.Verify(api => api.Replace(entry, updated), Times.Once);
    }
}

public class CollectionDiffTestCase
{
    public required TestOrderable[] OldValues { get; init; }
    public required TestOrderable[] NewValues { get; init; }
    public required List<CollectionDiffOperation> ExpectedOperations { get; init; }
}

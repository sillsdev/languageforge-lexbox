using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public class DiffCollectionTests
{
    [Fact]
    public async Task MatchingCollections_NoChangesAreGenerated()
    {
        // arrange
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());

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
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var value3 = Orderable(3, Guid.NewGuid());

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
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var value3 = Orderable(3, Guid.NewGuid());

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
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());

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
        var _1 = Orderable(1, Guid.NewGuid());
        var _2 = Orderable(2, Guid.NewGuid());
        var _3 = Orderable(3, Guid.NewGuid());
        var _4 = Orderable(4, Guid.NewGuid());
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

        var _5 = Orderable(5, Guid.NewGuid());
        var _6 = Orderable(6, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4, _5, _6],
            NewValues = [_2, _3, _1, _6, _4, _5],
            ExpectedOperations = [ // When 6+, moving the 2 outsides to middle is represented as such:
                Move(_1, Between(_3, _4)),
                Move(_6, Between(_1, _4)),
            ],
        }];

        var _7 = Orderable(7, Guid.NewGuid());
        var _8 = Orderable(8, Guid.NewGuid());
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
        diffApi.Current.Should().BeEquivalentTo(newValues);
        var expectedReplacements = oldValues.Join(newValues, o => o.Id, o => o.Id, (o, n) => (o, n)).ToList();
        diffApi.Replacements.Should().BeEquivalentTo(expectedReplacements);
        return (changeCount, diffApi.DiffOperations, diffApi.Replacements);
    }

    private static TestOrderable Orderable(double order, Guid? id = null)
    {
        return new TestOrderable()
        {
            Order = order,
            Id = id ?? Guid.NewGuid(),
        };
    }

    private static BetweenPosition Between(TestOrderable? previous, TestOrderable? next)
    {
        return Between(previous?.Id, next?.Id);
    }

    private static BetweenPosition Between(Guid? previous = null, Guid? next = null)
    {
        return new BetweenPosition
        {
            Previous = previous,
            Next = next
        };
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
}

public class CollectionDiffTestCase
{
    public required TestOrderable[] OldValues { get; init; }
    public required TestOrderable[] NewValues { get; init; }
    public required List<CollectionDiffOperation> ExpectedOperations { get; init; }
}

public record CollectionDiffOperation(TestOrderable Value, PositionDiffKind Kind, BetweenPosition? Between = null);

public class TestOrderable : IOrderable
{
    public required Guid Id { get; set; }
    public required double Order { get; set; }
}

public class TestOrderableDiffApi(TestOrderable[] before) : OrderableCollectionDiffApi<TestOrderable>
{
    public List<TestOrderable> Current { get; } = [.. before];
    public List<CollectionDiffOperation> DiffOperations = new();
    public List<(TestOrderable before, TestOrderable after)> Replacements = new();

    public Task<int> Add(TestOrderable value, BetweenPosition between)
    {
        DiffOperations.Add(new CollectionDiffOperation(value, PositionDiffKind.Add, between));
        return AddInternal(value, between);
    }

    private Task<int> AddInternal(TestOrderable value, BetweenPosition between)
    {
        if (between.Previous is not null)
        {
            var previousIndex = Current.FindIndex(v => v.Id == between.Previous);
            Current.Insert(previousIndex + 1, value);
        }
        else if (between.Next is not null)
        {
            var nextIndex = Current.FindIndex(v => v.Id == between.Next);
            Current.Insert(nextIndex, value);
        }
        else
        {
            Current.Add(value);
        }
        return Task.FromResult(1);
    }

    public Task<int> Remove(TestOrderable value)
    {
        DiffOperations.Add(new CollectionDiffOperation(value, PositionDiffKind.Remove));
        return RemoveInternal(value);
    }

    public Task<int> RemoveInternal(TestOrderable value)
    {
        var removeCount = Current.RemoveAll(v => v.Id == value.Id);
        removeCount.Should().Be(1);
        return Task.FromResult(1);
    }

    public async Task<int> Move(TestOrderable value, BetweenPosition between)
    {
        DiffOperations.Add(new CollectionDiffOperation(value, PositionDiffKind.Move, between));
        await RemoveInternal(value);
        await AddInternal(value, between);
        return 1;
    }

    public Task<int> Replace(TestOrderable before, TestOrderable after)
    {
        Replacements.Add((before, after));
        before.Id.Should().Be(after.Id);
        Current[Current.FindIndex(v => v.Id == before.Id)] = after;
        try
        {
            before.Should().BeEquivalentTo(after);
            return Task.FromResult(0);
        }
        catch
        {
            return Task.FromResult(1);
        }
    }
}

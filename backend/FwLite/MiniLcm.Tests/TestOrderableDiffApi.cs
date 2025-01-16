using MiniLcm.SyncHelpers;

namespace MiniLcm.Tests;

public class TestOrderableDiffApiTests
{
    [Fact]
    public async Task Add()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());

        // act
        var diffApi = new TestOrderableDiffApi([value1]);
        var position = new BetweenPosition(value1.Id, null);
        await diffApi.Add(value2, position);

        // assert
        diffApi.DiffOperations.Should().BeEquivalentTo([
            new CollectionDiffOperation(value2, PositionDiffKind.Add, position)
        ]);
        diffApi.Replacements.Should().BeEquivalentTo(Array.Empty<(TestOrderable, TestOrderable)>());
        diffApi.Current.Should().BeEquivalentTo([value1, value2]);
    }

    [Fact]
    public async Task Remove()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());

        // act
        var diffApi = new TestOrderableDiffApi([value1, value2]);
        await diffApi.Remove(value1);

        // assert
        diffApi.DiffOperations.Should().BeEquivalentTo([
            new CollectionDiffOperation(value1, PositionDiffKind.Remove)
        ]);
        diffApi.Replacements.Should().BeEquivalentTo(Array.Empty<(TestOrderable, TestOrderable)>());
        diffApi.Current.Should().BeEquivalentTo([value2]);
    }

    [Fact]
    public async Task Move()
    {
        // arrange
        var value1 = new TestOrderable(1, Guid.NewGuid());
        var value2 = new TestOrderable(2, Guid.NewGuid());
        var value3 = new TestOrderable(3, Guid.NewGuid());

        // act
        var diffApi = new TestOrderableDiffApi([value1, value2, value3]);
        var position = new BetweenPosition(value1.Id, value2.Id);
        await diffApi.Move(value3, position);

        // assert
        diffApi.DiffOperations.Should().BeEquivalentTo([
            new CollectionDiffOperation(value3, PositionDiffKind.Move, position)
        ]);
        diffApi.Replacements.Should().BeEquivalentTo(Array.Empty<(TestOrderable, TestOrderable)>());
        diffApi.Current.Should().BeEquivalentTo([value1, value3, value2]);
    }

    [Fact]
    public async Task Replace()
    {
        // arrange
        var oldValue = new TestOrderable(1, Guid.NewGuid());
        var newValue = new TestOrderable(2, oldValue.Id);

        // act
        var diffApi = new TestOrderableDiffApi([oldValue]);
        await diffApi.Replace(oldValue, newValue);

        // assert
        diffApi.DiffOperations.Should().BeEquivalentTo(Array.Empty<CollectionDiffOperation>());
        diffApi.Replacements.Should().BeEquivalentTo([(oldValue, newValue)]);
        diffApi.Current.Should().BeEquivalentTo([newValue]);
    }
}

public class TestOrderableDiffApi(TestOrderable[] before) : OrderableObjectWithIdCollectionDiffApi<TestOrderable>
{
    public List<TestOrderable> Current { get; } = [.. before];
    public List<CollectionDiffOperation> DiffOperations = [];
    public List<(TestOrderable before, TestOrderable after)> Replacements = [];

    public override Task<int> Add(TestOrderable value, BetweenPosition between)
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

    public override Task<int> Remove(TestOrderable value)
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

    public override async Task<int> Move(TestOrderable value, BetweenPosition between)
    {
        DiffOperations.Add(new CollectionDiffOperation(value, PositionDiffKind.Move, between));
        await RemoveInternal(value);
        await AddInternal(value, between);
        return 1;
    }

    public override Task<int> Replace(TestOrderable before, TestOrderable after)
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

public record CollectionDiffOperation(TestOrderable Value, PositionDiffKind Kind, BetweenPosition? Between = null);

public class TestOrderable(double order, Guid id) : IOrderable
{
    public Guid Id { get; set; } = id;
    public double Order { get; set; } = order;
}

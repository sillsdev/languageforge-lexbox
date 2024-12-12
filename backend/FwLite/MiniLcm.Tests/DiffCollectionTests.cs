using FluentAssertions.Execution;
using MiniLcm.SyncHelpers;
using Moq;

namespace MiniLcm.Tests;

public class DiffCollectionTests
{
    [Fact]
    public async Task MatchingCollections_NoChangesAreGenerated()
    {
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var (changeCount, _) = await Diff([value1, value2], [value1, value2]);

        changeCount.Should().Be(0);
    }

    [Fact]
    public async Task AddedValues()
    {
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var value3 = Orderable(3, Guid.NewGuid());
        var (changeCount, diffApi) = await Diff([value1], [value2, value1, value3]);

        changeCount.Should().Be(2);

        var x_v1 = Between(next: value1);
        diffApi.Verify(dApi => dApi.Add(value2, x_v1), Times.Once);

        var v1_x = Between(previous: value1);
        diffApi.Verify(dApi => dApi.Add(value3, v1_x), Times.Once);

        diffApi.Verify(dApi => dApi.Replace(value1, value1), Times.Once);
        diffApi.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RemovedValues()
    {
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var value3 = Orderable(3, Guid.NewGuid());
        var (changeCount, diffApi) = await Diff([value2, value1, value3], [value1]);

        changeCount.Should().Be(2);
        diffApi.Verify(dApi => dApi.Remove(value2), Times.Once);
        diffApi.Verify(dApi => dApi.Remove(value3), Times.Once);
        diffApi.Verify(dApi => dApi.Replace(value1, value1), Times.Once);
        diffApi.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SwappedValues()
    {
        var value1 = Orderable(1, Guid.NewGuid());
        var value2 = Orderable(2, Guid.NewGuid());
        var (changeCount, diffApi) = await Diff([value1, value2], [value2, value1]);

        changeCount.Should().Be(1);
        var v2_x = Between(previous: value2);
        diffApi.Verify(dApi => dApi.Move(value1, v2_x), Times.Once);
        diffApi.Verify(dApi => dApi.Replace(value1, value1), Times.Once);
        diffApi.Verify(dApi => dApi.Replace(value2, value2), Times.Once);
        diffApi.VerifyNoOtherCalls();
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
                new(_2) { From = 1, To = 1, Between = Between(previous: _3) },
                new(_1) { From = 0, To = 2, Between = Between(previous: _2) },
            ],
        }];
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4],
            NewValues = [_1, _4, _2, _3],
            ExpectedOperations = [
                new(_4) { From = 3, To = 1, Between = Between(_1, _2) },
            ],
        }];
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4],
            NewValues = [_2, _1, _4, _3],
            ExpectedOperations = [ // When only 4, moving the 2 outsides to middle is represented slightly differently:
                new(_1) { From = 0, To = 1, Between = Between(_2, _4) },
                new(_3) { From = 2, To = 3, Between = Between(_4, null) },
            ],
        }];

        var _5 = Orderable(5, Guid.NewGuid());
        var _6 = Orderable(6, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4, _5, _6],
            NewValues = [_2, _3, _1, _6, _4, _5],
            ExpectedOperations = [ // When 6+, moving the 2 outsides to middle is represented as such:
                new(_1) { From = 0, To = 2, Between = Between(_3, _4) },
                new(_6) { From = 5, To = 3, Between = Between(_1, _4) },
            ],
        }];

        var _7 = Orderable(7, Guid.NewGuid());
        var _8 = Orderable(8, Guid.NewGuid());
        yield return [new CollectionDiffTestCase
        {
            OldValues = [_1, _2, _3, _4, _5],
            NewValues = [_6, _8, _4, _2, _7],
            ExpectedOperations = [
                new(_1) { From = 0 },
                new(_3) { From = 2 },
                new(_5) { From = 4 },
                new(_6) { To = 0, Between = Between(next: _4) }, // (not next: _8, because _8 is not "stable")
                new(_8) { To = 1, Between = Between(_6, _4) },
                new(_2) { From = 1, To = 3, Between = Between(previous: _4) },
                new(_7) { To = 4, Between = Between(previous: _2) },
            ],
        }];
    }

    [Theory]
    [MemberData(nameof(CollectionDiffTestCaseData))]
    public async Task DiffTests(CollectionDiffTestCase testCase)
    {
        using (new AssertionScope("Check for silly test case mistakes"))
        {
            foreach (var operation in testCase.ExpectedOperations)
            {
                if (operation.From is not null)
                    testCase.OldValues[operation.From.Value].Should().Be(operation.Value);
                if (operation.To is not null)
                    testCase.NewValues[operation.To.Value].Should().Be(operation.Value);
                if (operation.Between?.Previous is not null)
                    testCase.NewValues.Should().ContainSingle(v => v.Id == operation.Between.Previous);
                if (operation.Between?.Next is not null)
                    testCase.NewValues.Should().ContainSingle(v => v.Id == operation.Between.Next);
            }
        }

        var (changeCount, diffApi) = await Diff(testCase.OldValues, testCase.NewValues);

        using var scope = new AssertionScope();

        changeCount.Should().Be(testCase.ExpectedOperations.Count);

        var expectedReplaceCount = testCase.OldValues.Select(v => v.Id).Intersect(testCase.NewValues.Select(v => v.Id)).Count();
        diffApi.Verify(dApi => dApi.Replace(It.IsAny<IOrderable>(), It.IsAny<IOrderable>()), Times.Exactly(expectedReplaceCount));

        foreach (var operation in testCase.ExpectedOperations)
        {
            try
            {
                if (operation.From is not null && operation.To is not null)
                {
                    operation.Between.Should().NotBeNull();
                    var movedValue = testCase.OldValues[operation.From.Value];
                    diffApi.Verify(
                        dApi => dApi.Move(
                            movedValue,
                            operation.Between
                        ),
                        Times.Once
                    );
                }
                else if (operation.From is not null)
                {
                    var removedValue = testCase.OldValues[operation.From.Value];
                    diffApi.Verify(
                        dApi => dApi.Remove(
                            removedValue
                        ),
                        Times.Once
                    );
                }
                else if (operation.To is not null)
                {
                    operation.Between.Should().NotBeNull();
                    var addedValue = testCase.NewValues[operation.To.Value];
                    diffApi.Verify(
                        dApi => dApi.Add(
                            addedValue,
                            operation.Between
                        ),
                        Times.Once
                    );
                }
            }
            catch (Exception ex)
            {
                scope.AddPreFormattedFailure($"{ex.Message} From: {operation.From} To: {operation.To}");
            }
        }

        diffApi.VerifyNoOtherCalls();
    }

    private static IOrderable Orderable(double order, Guid? id = null)
    {
        return new TestOrderable()
        {
            Order = order,
            Id = id ?? Guid.NewGuid(),
        };
    }

    private static BetweenPosition Between(IOrderable? previous = null, IOrderable? next = null)
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

    private static async Task<(int, Mock<OrderableCollectionDiffApi<IOrderable>>)> Diff(IOrderable[] oldValues, IOrderable[] newValues)
    {
        var diffApi = new Mock<OrderableCollectionDiffApi<IOrderable>>();
        diffApi.Setup(f => f.Add(It.IsAny<IOrderable>(), It.IsAny<BetweenPosition>()))
            .ReturnsAsync(1);
        diffApi.Setup(f => f.Remove(It.IsAny<IOrderable>()))
            .ReturnsAsync(1);
        diffApi.Setup(f => f.Move(It.IsAny<IOrderable>(), It.IsAny<BetweenPosition>()))
            .ReturnsAsync(1);
        diffApi.Setup(f => f.Replace(It.IsAny<IOrderable>(), It.IsAny<IOrderable>()))
            .Returns((IMiniLcmApi api, IOrderable oldValue, IOrderable newValue) =>
            {
                try
                {
                    oldValue.Should().BeEquivalentTo(newValue);
                    return Task.FromResult(0);
                }
                catch
                {
                    return Task.FromResult(1);
                }
            });

        var changeCount = await DiffCollection.DiffOrderable(oldValues, newValues, diffApi.Object);

        return (changeCount, diffApi);
    }
}

public class CollectionDiffTestCase
{
    public required IOrderable[] OldValues { get; init; }
    public required IOrderable[] NewValues { get; init; }
    public required List<CollectionDiffOperation> ExpectedOperations { get; init; }
}

public class CollectionDiffOperation(IOrderable value)
{
    public IOrderable Value { get; init; } = value;
    public int? From { get; init; }
    public int? To { get; init; }
    public BetweenPosition? Between { get; init; }
}

public class TestOrderable : IOrderable
{
    public required Guid Id { get; set; }
    public required double Order { get; set; }
}

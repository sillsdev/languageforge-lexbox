using MiniLcm.SyncHelpers;

namespace LcmCrdt.Tests;

public class OrderPickerTests : IAsyncLifetime
{
    private readonly MiniLcmApiFixture _fixture = new();
    private readonly Guid _entryId = Guid.NewGuid();

    public async Task InitializeAsync()
    {
        await _fixture.InitializeAsync();
        // A parent entry must exist to satisfy Sense.EntryId when we seed senses directly.
        await _fixture.Api.CreateEntry(new Entry { Id = _entryId, LexemeForm = { ["en"] = "test" } });
    }

    public Task DisposeAsync() => _fixture.DisposeAsync();

    public enum Variant
    {
        // OrderPicker.PickOrder(List<T>, ...) — synchronous, in-memory
        List,
        // OrderPicker.PickOrder(IQueryable<T>, ...) — async, against real SQLite
        Async
    }

    // Sentinel for a between reference whose id is not present among the siblings
    // (i.e. the referenced item was deleted by another user in the meantime).
    private const int Missing = -1;

    // Offset by 1 so index 0 does not map to Guid.Empty (which EF will not round-trip as a key).
    private static Guid ItemId(int index) => Guid.Parse($"00000000-0000-0000-0000-{index + 1:D12}");
    private static readonly Guid MissingId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");

    private static Guid? Ref(int? r) => r switch
    {
        null => null,
        Missing => MissingId,
        _ => ItemId(r.Value)
    };

    public record OrderScenario(
        string Name,
        double[] ExistingOrders,
        bool NullBetween,
        int? Previous,
        int? Next,
        double Expected)
    {
        public override string ToString() => Name;
    }

    private static IEnumerable<OrderScenario> AllScenarios()
    {
        // 1. No siblings, no between → first order is 1 (0 + 1)
        yield return new("empty, no between", [], NullBetween: true, null, null, 1);
        // 2. between is null → append after max (async optimized MaxAsync path)
        yield return new("no between → append", [1, 2, 3], NullBetween: true, null, null, 4);
        // 3. between {null,null} → append after max (async ToListAsync path, distinct from #2)
        yield return new("between {null,null} → append", [1, 2, 3], NullBetween: false, null, null, 4);
        // 4. previous only → just after previous
        yield return new("previous only", [10, 20], NullBetween: false, Previous: 0, null, 11);
        // 5. next only → just before next
        yield return new("next only", [10, 20], NullBetween: false, null, Next: 1, 19);
        // 6. previous < next → midpoint
        yield return new("previous < next → midpoint", [10, 20], NullBetween: false, Previous: 0, Next: 1, 15);
        // 7. previous > next (shifted past each other) → revert to previous + 1
        yield return new("inverted previous > next", [20, 10], NullBetween: false, Previous: 0, Next: 1, 21);
        // 8. previous == next order (distinct items, equal order) → previous + 1 (not strictly <)
        yield return new("equal orders", [10, 10], NullBetween: false, Previous: 0, Next: 1, 11);
        // 9. deleted references
        yield return new("both refs deleted → append", [1, 2, 3], NullBetween: false, Previous: Missing, Next: Missing, 4);
        yield return new("previous deleted, next present", [10, 20], NullBetween: false, Previous: Missing, Next: 1, 19);
        yield return new("next deleted, previous present", [10, 20], NullBetween: false, Previous: 0, Next: Missing, 11);
        // 10. siblings supplied out of Order sequence → result unaffected by list ordering
        yield return new("unordered siblings → midpoint", [30, 10, 20], NullBetween: false, Previous: 1, Next: 2, 15);
    }

    public static IEnumerable<object[]> Scenarios()
    {
        foreach (var scenario in AllScenarios())
        {
            yield return [Variant.List, scenario];
            yield return [Variant.Async, scenario];
        }
    }

    [Theory]
    [MemberData(nameof(Scenarios))]
    public async Task PickOrder_MatchesExpected(Variant variant, OrderScenario scenario)
    {
        BetweenPosition? between = scenario.NullBetween
            ? null
            : new BetweenPosition(Ref(scenario.Previous), Ref(scenario.Next));

        var result = variant switch
        {
            Variant.List => OrderPicker.PickOrder(BuildList(scenario.ExistingOrders), between),
            Variant.Async => await OrderPicker.PickOrder(await SeedAndQuery(scenario.ExistingOrders), between),
            _ => throw new ArgumentOutOfRangeException(nameof(variant))
        };

        result.Should().Be(scenario.Expected);
    }

    [Theory]
    [InlineData(Variant.List)]
    [InlineData(Variant.Async)]
    public async Task RepeatedInsertionIntoSameGap_StaysBetweenNeighborsAndDistinct(Variant variant)
    {
        // Two fixed neighbors at orders 0 and 1. We repeatedly insert between the lower
        // neighbor and the most-recently-inserted item, which is what happens when a user
        // keeps dropping items into the same spot — the gap halves each time.
        var lowerId = ItemId(0);
        var items = new List<Sense>
        {
            new() { Id = lowerId, EntryId = _entryId, Order = 0 },
            new() { Id = ItemId(1), EntryId = _entryId, Order = 1 },
        };
        if (variant == Variant.Async)
        {
            _fixture.DbContext.AddRange(items);
            await _fixture.DbContext.SaveChangesAsync();
        }

        var results = new List<double>();
        var upperId = ItemId(1);
        const int insertions = 18;
        for (var i = 2; i < 2 + insertions; i++)
        {
            var between = new BetweenPosition(lowerId, upperId);
            var order = variant switch
            {
                Variant.List => OrderPicker.PickOrder(items, between),
                Variant.Async => await OrderPicker.PickOrder(
                    _fixture.DbContext.Senses.Where(s => s.EntryId == _entryId), between),
                _ => throw new ArgumentOutOfRangeException(nameof(variant))
            };
            results.Add(order);

            var newId = ItemId(i);
            var sense = new Sense { Id = newId, EntryId = _entryId, Order = order };
            if (variant == Variant.Async)
            {
                _fixture.DbContext.Add(sense);
                await _fixture.DbContext.SaveChangesAsync();
            }
            else
            {
                items.Add(sense);
            }
            upperId = newId;
        }

        results.Should().OnlyContain(o => o > 0 && o < 1, "every insertion lands strictly between the two neighbors");
        results.Should().BeInDescendingOrder("each insertion bisects the shrinking gap above the lower neighbor");
        results.Distinct().Should().HaveCount(results.Count, "no two insertions collapse to the same order");
    }

    private static List<Sense> BuildList(double[] orders) =>
        orders.Select((order, index) => new Sense { Id = ItemId(index), Order = order }).ToList();

    private async Task<IQueryable<Sense>> SeedAndQuery(double[] orders)
    {
        for (var index = 0; index < orders.Length; index++)
        {
            _fixture.DbContext.Add(new Sense { Id = ItemId(index), EntryId = _entryId, Order = orders[index] });
        }
        await _fixture.DbContext.SaveChangesAsync();
        return _fixture.DbContext.Senses.Where(s => s.EntryId == _entryId);
    }
}

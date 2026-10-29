using LfClassicData;
using MiniLcm;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Entry = LfClassicData.Entities.Entry;

namespace Testing.LfClassic;

public class LfClassicQueryPipelineTests
{
    private static List<BsonDocument> RenderStages(QueryOptions options)
    {
        var pipeline = LfClassicMiniLcmApi.BuildQueryPipeline(options, "en", search: null);
        var registry = BsonSerializer.SerializerRegistry;
        return [.. pipeline.Render(new RenderArgs<Entry>(registry.GetSerializer<Entry>(), registry)).Documents];
    }

    [Fact]
    public void QueryAllOmitsLimitStage()
    {
        var stages = RenderStages(new QueryOptions(Count: QueryOptions.QueryAll, Offset: 10));

        stages.Should().ContainSingle(stage => stage.Contains("$skip"))
            .Which["$skip"].ToInt32().Should().Be(10);
        stages.Should().NotContain(stage => stage.Contains("$limit"));
    }

    [Fact]
    public void CountAddsLimitStage()
    {
        var stages = RenderStages(new QueryOptions(Count: 25, Offset: 10));

        stages.Should().ContainSingle(stage => stage.Contains("$skip"))
            .Which["$skip"].ToInt32().Should().Be(10);
        stages.Should().ContainSingle(stage => stage.Contains("$limit"))
            .Which["$limit"].ToInt32().Should().Be(25);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(false, -1)]
    public void SortFollowsRequestedDirection(bool ascending, int direction)
    {
        var stages = RenderStages(new QueryOptions(new SortOptions(SortField.Headword, "en", ascending)));

        var sort = stages.Should().ContainSingle(stage => stage.Contains("$sort")).Which["$sort"].AsBsonDocument;
        sort.Names.Should().HaveCount(3);
        sort.Values.Should().AllSatisfy(value => value.ToInt32().Should().Be(direction));
    }
}

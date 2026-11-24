using LcmCrdt;
using MiniLcmWeb;

namespace Testing.MiniLcmWeb;

public class GridifyFilterDocsTests
{
    [Fact]
    public void DocumentedFieldsMatchTheMapperTheServerFiltersWith()
    {
        var serverFields = new LcmCrdtConfig().Mapper.GetCurrentMaps()
            .Select(map => map.To.Parameters.Count > 1 ? $"{map.From}[ws]" : map.From);
        GridifyFilterDocs.Fields.Should().BeEquivalentTo(serverFields);
    }

    [Fact]
    public void DescriptionListsFieldsAndMarksWritingSystemFields()
    {
        GridifyFilterDocs.Description.Should()
            .Contain("- `LexemeForm[ws]`")
            .And.Contain("- `Senses.SemanticDomains.Code`")
            .And.Contain("- `MorphType`");
    }
}

using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.FakerOverrids;

public class WritingSystemIdOverride: AutoFakerOverride<WritingSystemId>
{
    public override void Generate(AutoFakerOverrideContext context)
    {
        var ws = context.Faker.Random.ArrayElement(WritingSystemCodes.ValidTwoLetterCodes);
        context.Instance = new WritingSystemId(ws);
    }
}

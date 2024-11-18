using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class WritingSystemIdOverride: AutoFakerOverride<WritingSystemId>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        var ws = context.Faker.Random.ArrayElement(WritingSystemCodes.ValidTwoLetterCodes);
        context.Instance = new WritingSystemId(ws);
    }
}

using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class WritingSystemIdOverride(string[]? validWs = null): AutoFakerOverride<WritingSystemId>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        var ws = context.Faker.Random.ArrayElement(validWs ?? WritingSystemCodes.ValidTwoLetterCodes);
        context.Instance = new WritingSystemId(ws);
    }
}

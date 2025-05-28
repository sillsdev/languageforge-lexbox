using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.AutoFakerHelpers;

public class MultiStringOverride(string[]? validWs = null): AutoFakerOverride<MultiString>
{
    public override bool Preinitialize => false;
    public override void Generate(AutoFakerOverrideContext context)
    {
        var target = context.Instance as MultiString;
        if (target is null)
        {
            context.Instance = target = new MultiString();
        }
        var wordsArray = context.Faker.Random.WordsArray(1, 4);
        foreach (var word in wordsArray)
        {
            var writingSystemId = context.Faker.Random.ArrayElement(validWs ?? WritingSystemCodes.ValidTwoLetterCodes);
            target[writingSystemId] = word;
        }
    }
}

public class RichMultiStringOverride(string[]? validWs = null): AutoFakerOverride<RichMultiString>
{
    public override bool Preinitialize => false;

    public override void Generate(AutoFakerOverrideContext context)
    {
        var target = context.Instance as RichMultiString;
        if (target is null)
        {
            context.Instance = target = new RichMultiString();
        }
        for (int i = 0; i < context.Faker.Random.Int(1, 4); i++)
        {
            var writingSystemId = context.Faker.Random.ArrayElement(validWs ?? WritingSystemCodes.ValidTwoLetterCodes);
            var wordsArray = context.Faker.Random.WordsArray(1, 4);
            var spans = new List<RichSpan>();
            foreach (var word in wordsArray)
            {
                if (context.Faker.Random.Bool())
                {
                    spans.Add(new()
                    {
                        Text = word,
                        Ws = writingSystemId
                    });
                }
                else
                {
                    spans.Add(context.AutoFaker.Generate<RichSpan>());
                }
            }

            target[writingSystemId] = new RichString(spans);
        }

    }
}

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
        var wordsArray = context.Faker.Random.WordsArray(1, 4);
        foreach (var word in wordsArray)
        {
            var writingSystemId = context.Faker.Random.ArrayElement(validWs ?? WritingSystemCodes.ValidTwoLetterCodes);
            target[writingSystemId] = new RichString(word);
        }
    }
}

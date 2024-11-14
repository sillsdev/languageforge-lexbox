using MiniLcm.Models;
using Soenneker.Utils.AutoBogus.Context;
using Soenneker.Utils.AutoBogus.Override;

namespace MiniLcm.Tests.FakerOverrids;

public class MultiStringOverride(string[]? validWs = null): AutoFakerOverride<MultiString>
{
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
